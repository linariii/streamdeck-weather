using System;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Settings
{
    public class WeatherDetailsPluginSettings : PluginSettingsBase
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("data")]
        public CurrentWeatherResult Data { get; set; }

        [JsonProperty(PropertyName = "lastSwipe")]
        public DateTime LastSwipe { get; set; }

        [JsonProperty("temperature")]
        public string TemperatureUnit { get; set; }

        [JsonProperty("speed")]
        public string SpeedUnit { get; set; }

        public static WeatherDetailsPluginSettings CreateDefaultSettings()
        {
            return new WeatherDetailsPluginSettings
            {
                LastRefresh = DateTime.MinValue,
                LastSwipe = DateTime.Now
            };
        }
    }
}