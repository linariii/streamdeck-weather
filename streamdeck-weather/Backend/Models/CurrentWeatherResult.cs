using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class CurrentWeatherResult
    {
        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("current")]
        public Current Current { get; set; }
    }
}