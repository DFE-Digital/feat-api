using Newtonsoft.Json;

namespace feat.api.Models.postcode.io;

public class Postcode
{
    [JsonProperty("postcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Code { get; set; }

    [JsonProperty("quality", NullValueHandling = NullValueHandling.Ignore)]
    public int? Quality { get; set; }

    [JsonProperty("longitude", NullValueHandling = NullValueHandling.Ignore)]
    public double? Longitude { get; set; }

    [JsonProperty("latitude", NullValueHandling = NullValueHandling.Ignore)]
    public double? Latitude { get; set; }

}

