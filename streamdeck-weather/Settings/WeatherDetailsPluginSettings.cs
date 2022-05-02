using System;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class WeatherDetailsPluginSettings : PluginSettingsBase
    {
        [JsonProperty("cities")]
        public string Cities { get; set; }

        [JsonProperty("data")]
        public CurrentWeatherResult Data { get; set; }

        [JsonProperty(PropertyName = "lastSwipe")]
        public DateTime LastSwipe { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        public static WeatherDetailsPluginSettings CreateDefaultSettings()
        {
            return new WeatherDetailsPluginSettings
            {
                LastRefresh = DateTime.MinValue,
                LastSwipe = DateTime.MinValue
            };
        }
    }
}