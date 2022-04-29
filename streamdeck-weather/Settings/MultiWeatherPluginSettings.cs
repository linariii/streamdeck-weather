using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class MultiWeatherPluginSettings : PluginSettingsBase
    {
        [JsonProperty("cities")]
        public string Cities { get; set; }

        [JsonProperty("data")]
        public List<CurrentWeatherResult> Data { get; set; }

        [JsonProperty(PropertyName = "lastSwipe")]
        public DateTime LastSwipe { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        public static MultiWeatherPluginSettings CreateDefaultSettings()
        {
            return new MultiWeatherPluginSettings
            {
                LastRefresh = DateTime.MinValue,
                LastSwipe = DateTime.MinValue
            };
        }
    }
}