using System;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class CurrentWeatherPluginSettings : PluginSettingsBase
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("data")]
        public CurrentWeatherResult Data { get; set; }

        public static CurrentWeatherPluginSettings CreateDefaultSettings()
        {
            return new CurrentWeatherPluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
        }
    }
}