using Newtonsoft.Json;

namespace Weather.Actions
{
    public class GlobalSettings
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }
        public static GlobalSettings CreateDefaultSettings()
        {
            return new GlobalSettings();
        }   
    }
}