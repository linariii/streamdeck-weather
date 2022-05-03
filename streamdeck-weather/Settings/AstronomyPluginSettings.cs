using System;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class AstronomyPluginSettings : PluginSettingsBase
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("data")]
        public AstronomyResult Data { get; set; }

        public static AstronomyPluginSettings CreateDefaultSettings()
        {
            return new AstronomyPluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
        }
    }
}