using System;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class WeatherForecastPluginSettings : PluginSettingsBase
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("data")]
        public ForecastResult Data { get; set; }

        [JsonProperty(PropertyName = "lastSwipe")]
        public DateTime LastSwipe { get; set; }

        public static WeatherForecastPluginSettings CreateDefaultSettings()
        {
            return new WeatherForecastPluginSettings
            {
                LastRefresh = DateTime.MinValue,
                LastSwipe = DateTime.Now
            };
        }
    }
}