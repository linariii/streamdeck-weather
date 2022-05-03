using System.Collections.Generic;
using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class ForecastDay
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("date_epoch")]
        public int DateEpoch { get; set; }

        [JsonProperty("day")]
        public Day Day { get; set; }

        [JsonProperty("astro")]
        public Astro Astro { get; set; }

        //[JsonProperty("hour")]
        //public List<Hour> Hour { get; set; }
    }
}