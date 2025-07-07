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
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace feat.api.Services;

public class SearchService: ISearchService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly SearchClient _aiSearchClient;
    private readonly AzureOptions _azureOptions;
    private readonly EmbeddingClient _embeddingClient;
    private readonly  HttpClientRepository _httpClientRepository;
    
    public SearchService(IOptionsMonitor<AzureOptions> options, HttpClientRepository httpClientRepository)
    {
        _azureOptions = options.CurrentValue;
            
        // Setup search client
        Uri searchEndpoint = new Uri(_azureOptions.AISearchURL);
        AzureKeyCredential searchCredential = new AzureKeyCredential(_azureOptions.AISearchKey);
        SearchClientOptions searchClientOptions = new SearchClientOptions();
        searchClientOptions.Diagnostics.IsLoggingContentEnabled = true;
        _aiSearchClient = new SearchClient(searchEndpoint, _azureOptions.AISearchIndex, searchCredential, searchClientOptions);
            
            
        // Setup OpenAI client
        Uri embeddingsEndpoint = new Uri(_azureOptions.OpenAIEndpoint);
        AzureKeyCredential openAiCredential = new AzureKeyCredential(_azureOptions.OpenAIKey);
        _openAiClient = new AzureOpenAIClient(embeddingsEndpoint, openAiCredential);
        _embeddingClient = _openAiClient.GetEmbeddingClient("text-embedding-ada-002");

        // Create our http client for geo lookups
        _httpClientRepository = httpClientRepository;
    }
    
    public async Task<FindAResponse> HybridSearch(FindARequest request)
    {
        Geolocation? geolocation = null;

            if (!string.IsNullOrEmpty(request.Location))
            {
                

                Regex postcodeCheck = new Regex(@"^[a-z]{1,2}\d[a-z\d]?\s*\d[a-z]{2}$", RegexOptions.IgnoreCase);

                // If it looks like a UK postcode, let's try and find our lat/long
                if (postcodeCheck.IsMatch(request.Location))
                {
                    var response =
                        await _httpClientRepository.GetAsync<PostcodeResult>(
                            $"https://api.postcodes.io/postcodes/{request.Location}");
                    if (response != null)
                    {
                        geolocation = new Geolocation
                        {
                            Latitude = response.Result.Latitude.GetValueOrDefault(),
                            Longitude = response.Result.Longitude.GetValueOrDefault()
                        };
                    }
                }
                else
                {
                    var response =
                        await _httpClientRepository.GetAsync<PlaceResult>(
                            $"https://api.postcodes.io/places/?q={request.Location}&limit=1");
                    if (response is { Result.Count: > 0 })
                    {
                        geolocation = new Geolocation
                        {
                            Latitude = response.Result[0].Latitude.GetValueOrDefault(),
                            Longitude = response.Result[0].Longitude.GetValueOrDefault()
                        };
                    }
                }

                
            }
            // As our radius is specified in miles, 
            double radiusInKm = request.Radius * 1.60934;
            
            var embedding = await _embeddingClient.GenerateEmbeddingAsync(request.Query);

            string filter = "";
            string orderby = "";

            if (geolocation != null)
            {
                filter = $"(geo.distance(GEOPOINT_LATLONG, geography'POINT({geolocation.Latitude} {geolocation.Longitude})') lt {radiusInKm})";
                if (request.IncludeOnlineCourses)
                    filter += " or (DELIVERY_MODE eq 'Online')";
                else
                    filter += " and (DELIVERY_MODE ne 'Online')";
            }
            else
            {
                filter = request.IncludeOnlineCourses ? "DELIVERY_MODE eq 'Online'" : "DELIVERY_MODE ne 'Online'";
            }

            if (geolocation != null && request.OrderBy == OrderBy.Distance)
                orderby = $"geo.distance(GEOPOINT_LATLONG, geography'POINT({geolocation.Latitude} {geolocation.Longitude})')";

            var embeddings = embedding.Value.ToFloats();
            
            var search = await _aiSearchClient.SearchAsync<AiSearchCourse>(
                request.Query,
                new SearchOptions
                {
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries =
                        {
                            new VectorizedQuery(embeddings)
                            {
                                KNearestNeighborsCount = _azureOptions.KNN,
                                Fields = { "WHO_THIS_COURSE_IS_FOR_Vector" },
                                Weight = 10
                            },
                            new VectorizedQuery(embeddings)
                            {
                                KNearestNeighborsCount = _azureOptions.KNN,
                                Fields = { "COURSE_NAME_Vector" },
                                Weight = 10
                            }
                        },
                        
                    },
                    SemanticSearch = new SemanticSearchOptions()
                    {
                        Debug = request.Debug.GetValueOrDefault(false) ? QueryDebugMode.All : QueryDebugMode.Disabled
                    },
                    SearchFields =
                    {
                        nameof(AiSearchCourse.COURSE_NAME), 
                        nameof(AiSearchCourse.WHO_THIS_COURSE_IS_FOR), 
                        // nameof(AiSearchCourse.LEARNING_AIM_TITLE),
                        // nameof(AiSearchCourse.LEARNING_DIRECT_CLASSIFICATION),
                        // nameof(AiSearchCourse.TOPIC_MODELING),
                        //nameof(AiSearchCourse.SSAT1),
                        //nameof(AiSearchCourse.SSAT2)
                    },
                    Facets =
                    {
                        nameof(AiSearchCourse.DELIVERY_MODE), 
                        nameof(AiSearchCourse.STUDY_MODE), 
                        nameof(AiSearchCourse.SECTOR), 
                        nameof(AiSearchCourse.DATA_SOURCE), 
                        nameof(AiSearchCourse.LEVEL),
                        nameof(AiSearchCourse.QUALIFICATION_TYPE)
                    },
                    Filter = filter,
                    IncludeTotalCount = true,
                    Size = request.PageSize,
                    Skip = (request.Page - 1) * request.PageSize,
                    SessionId = request.SessionId,
                    OrderBy = { orderby }
                    
                }
                );


            var results = search.Value;

            var result = new FindAResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Courses = [],
                Facets = results.Facets
            };

            await foreach (var searchResult in search.Value.GetResultsAsync())
            {
                if (searchResult.DocumentDebugInfo != null)
                {
                    result.Courses.Add(new Course(searchResult.Document, searchResult.Score, geolocation,
                        searchResult.DocumentDebugInfo));
                }
                else
                {
                    result.Courses.Add(new Course(searchResult.Document, searchResult.Score, geolocation));
                }

            }
            
            result.Total = results.TotalCount;

            return result;
    }
}