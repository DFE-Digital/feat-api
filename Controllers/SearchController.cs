using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using feat.api.Configuration;
using feat.api.Enums;
using feat.api.Models;
using feat.api.Models.postcode.io;
using feat.api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace feat.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly AzureOpenAIClient openAIClient;
        private readonly SearchClient aiSearchClient;
        private readonly AzureOptions azureOptions;
        private readonly EmbeddingClient embeddingClient;
        private readonly  HttpClientRepository httpClientRepository;
        
        public SearchController(IOptionsMonitor<AzureOptions> options, HttpClientRepository httpClientRepository)
        {
            azureOptions = options.CurrentValue;
            
            // Setup search client
            Uri searchEndpoint = new Uri(azureOptions.AISearchURL);
            AzureKeyCredential searchCredential = new AzureKeyCredential(azureOptions.AISearchKey);
            aiSearchClient = new SearchClient(searchEndpoint, azureOptions.AISearchIndex, searchCredential);
            
            
            // Setup OpenAI client
            Uri embeddingsEndpoint = new Uri(azureOptions.OpenAIEndpoint);
            AzureKeyCredential openAiCredential = new AzureKeyCredential(azureOptions.OpenAIKey);
            openAIClient = new AzureOpenAIClient(embeddingsEndpoint, openAiCredential);
            embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-ada-002");

            // Create our http client for geo lookups
            this.httpClientRepository = httpClientRepository;
        }
        
        
        // POST api/<SearchController>
        [HttpPost]
        public async Task<FindAResponse> Post([FromBody] FindARequest request)
        {
           var geolocation = new Geolocation();

            Regex postcodeCheck = new Regex(@"^[a-z]{1,2}\d[a-z\d]?\s*\d[a-z]{2}$", RegexOptions.IgnoreCase);

            // If it looks like a UK postcode, let's try and find our lat/long
            if (postcodeCheck.IsMatch(request.Location))
            {
                var response =
                    await httpClientRepository.GetAsync<PostcodeResult>(
                        $"https://api.postcodes.io/postcodes/{request.Location}");
                if (response != null)
                {
                    geolocation.Latitude = response.Result.Latitude.GetValueOrDefault();
                    geolocation.Longitude = response.Result.Longitude.GetValueOrDefault();
                }
            }
            else
            {
                var response =
                    await httpClientRepository.GetAsync<PlaceResult>(
                        $"https://api.postcodes.io/places/?q={request.Location}&limit=1");
                if (response is { Result.Count: > 0 })
                {
                    geolocation.Latitude = response.Result[0].Latitude.GetValueOrDefault();
                    geolocation.Longitude = response.Result[0].Longitude.GetValueOrDefault();
                }
            }

            // As our radius is specified in miles, 
            double radiusInKm = request.Radius * 1.60934;

            var embedding = await embeddingClient.GenerateEmbeddingAsync(request.Query);

            string filter =
                $"(geo.distance(GEOPOINT_LATLONG, geography'POINT({geolocation.Latitude} {geolocation.Longitude})') lt {radiusInKm})";

            ReadOnlyMemory<float> vectorizedResult = embedding.Value.ToFloats();
            

            var search = await aiSearchClient.SearchAsync<AiSearchCourse>(
                new SearchOptions
                {
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries =
                        {
                            new VectorizedQuery(vectorizedResult)
                            {
                                KNearestNeighborsCount = 50,
                                Fields = { "WHO_THIS_COURSE_IS_FOR_Vector" },
                            }
                        }
                    },
                    Filter = request.IncludeOnlineCourses ? filter + " or (DELIVERY_MODE eq 'Online')" : filter,
                    IncludeTotalCount = true,
                    Size = request.PageSize,
                    SearchMode = SearchMode.All,
                    Skip = (request.Page - 1) * request.PageSize,
                    SessionId = HttpContext.Session.Id,
                    OrderBy =
                    {
                        request.OrderBy switch
                        {
                            OrderBy.Distance =>
                                $"geo.distance(GEOPOINT_LATLONG, geography'POINT({geolocation.Latitude} {geolocation.Longitude})')",
                            _ => ""
                        }
                    }
                });


            var results = search.Value;

            var result = new FindAResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Courses = []
            };

            await foreach (var searchResult in search.Value.GetResultsAsync())
            {
                result.Courses.Add(new Course(searchResult.Document, searchResult.Score, geolocation));
            }
            
            result.Total = results.TotalCount;

            return result;
        }

        [HttpGet]
        public async Task<FindAResponse> Get(
            [FromQuery] string query,
            [FromQuery] string location,
            [FromQuery] double radius = 5,
            [FromQuery] bool includeOnlineCourses = false,
            [FromQuery] OrderBy orderBy = OrderBy.Relevance,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 3
        )
        {
            var request = new FindARequest()
            {
                Query = query,
                Location = location,
                Radius = radius,
                IncludeOnlineCourses = includeOnlineCourses,
                OrderBy = orderBy,
                Page = page,
                PageSize = pageSize
            };
            return await Post(request);
        }

    }
}
