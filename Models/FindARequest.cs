using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using feat.api.Enums;

namespace feat.api.Models;

public class FindARequest
{
    [Required]
    public required string Query { get; set; }

    [Required]
    public required string Location { get; set; }

    public bool IncludeOnlineCourses { get; set; } = false;

    public double Radius { get; set; } = 10;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderBy OrderBy { get; set; } = OrderBy.Relevance;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

}