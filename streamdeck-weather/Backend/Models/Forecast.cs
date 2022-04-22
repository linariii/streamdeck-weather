using System.Collections.Generic;
using Newtonsoft.Json;

namespace Weather.Backend.Models
{
    public class Forecast
    {
        [JsonProperty("forecastday")]
        public List<ForeCastDay> Forecastday { get; set; }
    }
}