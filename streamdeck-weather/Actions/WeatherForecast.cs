using System;
using BarRaider.SdTools;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.forecast")]
    public class WeatherForecast : ActionBase
    {
        private DateTime _lastSwipe = DateTime.Now;
        public WeatherForecast(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = WeatherForecastPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<WeatherForecastPluginSettings>();
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        protected WeatherForecastPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as WeatherForecastPluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => BaseSettings = value;
        }

        public override void KeyPressed(KeyPayload payload) { }


        public override void OnTick() { }

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
                if (Tools.AutoPopulateSettings(Settings, payload.Settings) > 0)
                {
                    Settings.LastRefresh = DateTime.MinValue;
                    _lastSwipe = DateTime.Now;
                    await SaveSettings();
                }
            }
        }
    }
}