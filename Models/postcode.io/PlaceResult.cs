using Newtonsoft.Json;

namespace feat.api.Models.postcode.io;

public class PlaceResult
{
    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public int? Status { get; set; }
    
    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public List<Place> Result { get; set; }
}