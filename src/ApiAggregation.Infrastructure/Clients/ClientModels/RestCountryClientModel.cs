using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ApiAggregation.Infrastructure.Clients.ClientModels
{
    public class RestCountryClientModel
    {
        [JsonProperty("name")]
        public Name Name { get; set; } =default!;

        [JsonProperty("capital")]
        public List<string> Capital { get; set; } = default!;
    }

    public class Name
    {
        [JsonProperty("common")]
        public string Common { get; set; } = default!;

        [JsonProperty("official")]
        public string Official { get; set; } = default!;

        [JsonProperty("nativeName")]
        public NativeName NativeName { get; set; } = default!;
    }

    public class NativeName
    {
        [JsonProperty("fra")]
        public Fra Fra { get; set; } = default!;
    }

    public class Fra
    {
        [JsonProperty("official")]
        public string Official { get; set; } = default!;

        [JsonProperty("common")]
        public string Common { get; set; } = default!;
    }
}