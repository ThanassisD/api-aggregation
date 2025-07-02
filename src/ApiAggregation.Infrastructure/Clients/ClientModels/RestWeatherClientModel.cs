using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApiAggregation.Infrastructure.Clients.ClientModels
{
    public class RestWeatherClientModel
    {
        [JsonProperty("coord")]
        public Coord Coord { get; set; } =default!;

        [JsonProperty("weather")]
        public List<Weather>? Weather { get; set; } =default!;

        [JsonProperty("base")]
        public string Base { get; set; } =default!;

        [JsonProperty("main")]
        public Main Main { get; set; } =default!;

        [JsonProperty("visibility")]
        public long Visibility { get; set; } =default!;

        [JsonProperty("wind")]
        public Wind Wind { get; set; } =default!;

        [JsonProperty("clouds")]
        public Clouds Clouds { get; set; } =default!;

        [JsonProperty("dt")]
        public long Dt { get; set; } =default!;

        [JsonProperty("sys")]
        public Sys Sys { get; set; } =default!;

        [JsonProperty("timezone")]
        public long Timezone { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } =default!;

        [JsonProperty("cod")]
        public long Cod { get; set; }
    }

    public class Clouds
    {
        [JsonProperty("all")]
        public long All { get; set; }
    }

    public class Coord
    {
        [JsonProperty("lon")]
        public double Lon { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }
    }

    public class Main
    {
        [JsonProperty("temp")]
        public double Temp { get; set; }

        [JsonProperty("feels_like")]
        public double FeelsLike { get; set; }

        [JsonProperty("temp_min")]
        public double TempMin { get; set; }

        [JsonProperty("temp_max")]
        public double TempMax { get; set; }

        [JsonProperty("pressure")]
        public long Pressure { get; set; }

        [JsonProperty("humidity")]
        public long Humidity { get; set; }

        [JsonProperty("sea_level")]
        public long SeaLevel { get; set; }

        [JsonProperty("grnd_level")]
        public long GrndLevel { get; set; }
    }

    public class Sys
    {
        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; } =default!;

        [JsonProperty("sunrise")]
        public long Sunrise { get; set; }

        [JsonProperty("sunset")]
        public long Sunset { get; set; }
    }

    public class Weather
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("main")]
        public string Main { get; set; } =default!;

        [JsonProperty("description")]
        public string Description { get; set; } =default!;

        [JsonProperty("icon")]
        public string Icon { get; set; } =default!;
    }

    public class Wind
    {
        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("deg")]
        public long Deg { get; set; }
    }
}
