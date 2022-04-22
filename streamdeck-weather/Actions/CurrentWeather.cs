using System;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using Weather.Backend.Models;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.current.weather")]
    public class CurrentWeather : PluginBase
    {
        private const int FetchCooldownSec = 300; // 5 min
        private PluginSettings _settings;
        private GlobalSettings _globalSettings;

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

        public CurrentWeather(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
                _globalSettings = GlobalSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
                _settings = payload.Settings.ToObject<PluginSettings>();
                if(_settings != null)
                    _settings.LastRefresh = DateTime.MinValue;
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            await SaveSettings();
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings: {payload.Settings}");
                var settings = payload.Settings.ToObject<GlobalSettings>();
                if (settings != null && _globalSettings != null)
                {
                    var updated = false;
                    if (settings.ApiKey != _globalSettings.ApiKey)
                    {
                        _globalSettings.ApiKey = settings.ApiKey;
                        updated = true;
                    }

                    await SaveGlobalSettings(updated);
                }
            }
        }

        private async Task SaveSettings()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (_globalSettings != null)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(_globalSettings)}");
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(_globalSettings), triggerDidReceiveGlobalSettings);
            }
        }
    }
}