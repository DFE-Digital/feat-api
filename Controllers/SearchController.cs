using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using feat.api.Configuration;
using feat.api.Enums;
using feat.api.Models;
using feat.api.Models.postcode.io;
using feat.api.Repositories;
using feat.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace feat.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController (ISearchService searchService): ControllerBase
    {
       // POST api/<SearchController>
        [HttpPost]
        public async Task<FindAResponse> Post([FromBody] FindARequest request)
        {
            return await searchService.HybridSearch(request);
        }

        [HttpGet]
        public async Task<FindAResponse> Get(
            [FromQuery] string query,
            [FromQuery] string location,
            [FromQuery] double radius = 5,
            [FromQuery] bool includeOnlineCourses = false,
            [FromQuery] OrderBy orderBy = OrderBy.Relevance,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
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
            return await searchService.HybridSearch(request);
        }

    }
}
