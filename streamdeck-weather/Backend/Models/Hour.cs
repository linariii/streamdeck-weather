using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class Hour
    {
        [JsonProperty("time_epoch")]
        public int TimeEpoch { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("temp_c")]
        public float TempC { get; set; }

        [JsonProperty("temp_f")]
        public float TempF { get; set; }

        [JsonProperty("is_day")]
        public int IsDay { get; set; }

        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("wind_mph")]
        public float WindMph { get; set; }

        [JsonProperty("wind_kph")]
        public float WindKph { get; set; }

        [JsonProperty("wind_degree")]
        public int WindDegree { get; set; }

        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }

        [JsonProperty("pressure_mb")]
        public float PressureMb { get; set; }

        [JsonProperty("pressure_in")]
        public float PressureIn { get; set; }

        [JsonProperty("precip_mm")]
        public float PrecipMm { get; set; }

        [JsonProperty("precip_in")]
        public float PrecipIn { get; set; }

        [JsonProperty("humidity")]
        public int Humidity { get; set; }

        [JsonProperty("cloud")]
        public int Cloud { get; set; }

        [JsonProperty("feelslike_c")]
        public float FeelslikeC { get; set; }

        [JsonProperty("feelslike_f")]
        public float FeelslikeF { get; set; }

        [JsonProperty("windchill_c")]
        public float WindchillC { get; set; }

        [JsonProperty("windchill_f")]
        public float WindchillF { get; set; }

        [JsonProperty("heatindex_c")]
        public float HeatindexC { get; set; }

        [JsonProperty("heatindex_f")]
        public float HeatindexF { get; set; }

        [JsonProperty("dewpoint_c")]
        public float DewpointC { get; set; }

        [JsonProperty("dewpoint_f")]
        public float DewpointF { get; set; }

        [JsonProperty("will_it_rain")]
        public int WillItRain { get; set; }

        [JsonProperty("chance_of_rain")]
        public int ChanceOfRain { get; set; }

        [JsonProperty("will_it_snow")]
        public int WillItSnow { get; set; }

        [JsonProperty("chance_of_snow")]
        public int ChanceOfSnow { get; set; }

        [JsonProperty("vis_km")]
        public float VisKm { get; set; }

        [JsonProperty("vis_miles")]
        public float VisMiles { get; set; }

        [JsonProperty("gust_mph")]
        public float GustMph { get; set; }

        [JsonProperty("gust_kph")]
        public float GustKph { get; set; }

        [JsonProperty("uv")]
        public float Uv { get; set; }
    }
}