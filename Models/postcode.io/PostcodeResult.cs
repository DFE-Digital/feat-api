using Newtonsoft.Json;

namespace feat.api.Models.postcode.io;

public class PostcodeResult

{
    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public int? Status { get; set; }

    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public Postcode Result { get; set; }

}