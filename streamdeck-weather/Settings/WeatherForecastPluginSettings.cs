using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class WeatherForecastPluginSettings : PluginSettingsBase
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("data")]
        public List<ForecastDay> Data { get; set; }

        public static WeatherForecastPluginSettings CreateDefaultSettings()
        {
            return new WeatherForecastPluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
        }
    }
}