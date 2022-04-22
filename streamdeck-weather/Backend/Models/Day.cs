using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class Day
    {
        [JsonProperty("maxtemp_c")]
        public float MaxTempC { get; set; }

        [JsonProperty("maxtemp_f")]
        public float MaxTempF { get; set; }

        [JsonProperty("mintemp_c")]
        public float MinTempC { get; set; }

        [JsonProperty("mintemp_f")]
        public float MinTempF { get; set; }

        [JsonProperty("avgtemp_c")]
        public float AvgTempC { get; set; }

        [JsonProperty("avgtemp_f")]
        public float AvgTempF { get; set; }

        [JsonProperty("maxwind_mph")]
        public float MaxWindMph { get; set; }

        [JsonProperty("maxwind_kph")]
        public float MaxWindKph { get; set; }

        [JsonProperty("totalprecip_mm")]
        public float TotalPrecipMm { get; set; }

        [JsonProperty("totalprecip_in")]
        public float TotalPrecipIn { get; set; }

        [JsonProperty("avgvis_km")]
        public float AvgVisKm { get; set; }

        [JsonProperty("avgvis_miles")]
        public float AvgVisMiles { get; set; }

        [JsonProperty("avghumidity")]
        public float AvgHumidity { get; set; }

        [JsonProperty("daily_will_it_rain")]
        public int DailyWillItRain { get; set; }

        [JsonProperty("daily_chance_of_rain")]
        public int DailyChanceOfRain { get; set; }

        [JsonProperty("daily_will_it_snow")]
        public int DailyWillItSnow { get; set; }

        [JsonProperty("daily_chance_of_snow")]
        public int DailyChanceOfSnow { get; set; }

        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("uv")]
        public float Uv { get; set; }
    }
}