using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class Astronomy
    {
        [JsonProperty("astro")]
        public Astro Astro { get; set; }
    }
}