using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class AstronomyResult
    {
        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("astronomy")]
        public Astronomy Astronomy { get; set; }
    }
}