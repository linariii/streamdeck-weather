using System;
using BarRaider.SdTools;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.details")]
    public class WeatherDetails : ActionBase
    {
        public WeatherDetails(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = WeatherDetailsPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<WeatherDetailsPluginSettings>();
                if (Settings != null)
                    Settings.LastSwipe = DateTime.Now;
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        protected WeatherDetailsPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as WeatherDetailsPluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => BaseSettings = value;
        }

        public override async void KeyPressed(KeyPayload payload)
        {

        }

        public override async void OnTick()
        {
           
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
#if DEBUG           
            Logger.Instance.LogMessage(TracingLevel.INFO, "ReceivedSettings");
#endif
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings: {payload.Settings}");
#endif
                if (Tools.AutoPopulateSettings(BaseSettings, payload.Settings) > 0)
                {
                    Settings.LastRefresh = DateTime.MinValue;
                    Settings.LastSwipe = DateTime.Now;
                    await SaveSettings();
                }
            }
        }
    }
}