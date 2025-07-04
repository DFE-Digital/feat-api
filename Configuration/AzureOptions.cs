namespace feat.api.Configuration;

public class AzureOptions
{
    public const string Azure = "Azure";

    public string OpenAIKey { get; set; } = string.Empty;
    
    public string OpenAIEndpoint { get; set; } = string.Empty;
    
    public string AISearchKey { get; set; } = string.Empty;
    
    public string AISearchURL { get; set; } = string.Empty;
    
    public string AISearchIndex { get; set; } = string.Empty;

    public float? Weight { get; set; } = 1.3f;
    
    public int? KNN { get; set; } = 115;
    
}