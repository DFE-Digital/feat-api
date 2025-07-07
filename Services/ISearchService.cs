using feat.api.Enums;
using feat.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace feat.api.Services;

public interface ISearchService
{
    Task<FindAResponse> HybridSearch(FindARequest request);
}