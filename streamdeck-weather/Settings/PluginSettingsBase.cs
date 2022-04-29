using System;
using Newtonsoft.Json;

namespace Weather.Settings
{
    public class PluginSettingsBase
    {
        [JsonProperty(PropertyName = "lastRefresh")]
        public DateTime LastRefresh { get; set; }
    }
}