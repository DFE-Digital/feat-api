using Newtonsoft.Json;

namespace feat.api.Models.postcode.io;

public class Place
{
        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }

        [JsonProperty("name_1", NullValueHandling = NullValueHandling.Ignore)]
        public string Name1 { get; set; }
        
        [JsonProperty("name_2", NullValueHandling = NullValueHandling.Ignore)]
        public object Name2 { get; set; }

        [JsonProperty("longitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Longitude { get; set; }

        [JsonProperty("latitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Latitude { get; set; }
        
    }
