using System;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.current")]
    public class CurrentWeather : ActionBase
    {
        public CurrentWeather(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = CurrentWeatherPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<CurrentWeatherPluginSettings>();
            }
        }

        protected CurrentWeatherPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as CurrentWeatherPluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => BaseSettings = value;
        }

        public override async void OnTick()
        {
            if (GlobalSettings == null || Settings == null)
                return;

            if (string.IsNullOrWhiteSpace(GlobalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(Settings.City))
                return;

            if (IsRunning > 0)
                return;

            var locked = false;
            try
            {
                try { }
                finally
                {
                    locked = Interlocked.CompareExchange(ref IsRunning, 1, 0) == 0;
                }

                if (locked)
                {
                    var data = await ShouldLoadWeatherData(Settings.City);
                    if (data != null)
                    {
                        Settings.Data = data;
                        await DrawKeyImage();
                    }
                    Settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref IsRunning, 0);
            }
        }

        private async Task DrawKeyImage()
        {
            var showTitle = !string.IsNullOrWhiteSpace(Settings.DisplayName) && Settings.DisplayName == "1";
            var title = !string.IsNullOrWhiteSpace(Settings.Data.Location?.Name)
                ? Settings.Data.Location?.Name
                : Settings.City;

            var data = !string.IsNullOrWhiteSpace(Settings.Unit) && Settings.Unit == "f"
                ? $"{Math.Round(Settings.Data.Current.TempF, 0)} °F"
                : $"{Math.Round(Settings.Data.Current.TempC, 0)} °C";

            var iconPath = GetConditonIconPath(Settings.Data);

            await DrawWeatherKeyImage(showTitle, title, data, iconPath);
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
                if (Tools.AutoPopulateSettings(Settings, payload.Settings) > 0)
                {
                    Settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }
    }
}