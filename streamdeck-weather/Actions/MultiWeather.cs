using System;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using Weather.Backend.Models;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.multi.weather")]
    public class MultiWeather : ActionBase
    {
        private const int FetchCooldownSec = 300; // 5 min
        private PluginSettings _settings;

        private class PluginSettings
        {
            public string CityName { get; set; }
            public CurrentWeatherResult Data { get; set; }
            public DateTime LastRefresh { get; set; }
            public static PluginSettings CreateDefaultSettings()
            {
                return new PluginSettings
                {
                    LastRefresh = DateTime.MinValue
                };
            }
        }

        public MultiWeather(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                _settings = payload.Settings.ToObject<PluginSettings>();
                if(_settings != null)
                    _settings.LastRefresh = DateTime.MinValue;
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        public override void Dispose() { }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            await SaveSettings();
        }

        private async Task SaveSettings()
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
#endif
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }
    }
}