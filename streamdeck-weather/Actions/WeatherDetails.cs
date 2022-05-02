using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Weather.Backend;
using Weather.Enums;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.details")]
    public class WeatherDetails : ActionBase
    {
        private const int NumberOfSlides = 6;
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
            if (GlobalSettings == null || Settings == null)
                return;

            if (string.IsNullOrWhiteSpace(GlobalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(Settings.City))
                return;

            if (Settings.Data == null)
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
                    await DrawNext();
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref IsRunning, 0);
            }
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
                    await ShouldLoadData();
                    if (Settings.Data != null)
                    {
                        await ShouldDrawNext();
                    }
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref IsRunning, 0);
            }
        }

        private async Task ShouldDrawNext()
        {
            if (!IsInitialized)
            {
                await DrawNext();
            }
            else if ((DateTime.Now - Settings.LastSwipe).TotalSeconds > SwipeCooldownSec)
            {
                await DrawNext();
            }
        }

        private async Task DrawNext()
        {
            if (Settings.Data == null)
                return;

            IsInitialized = true;
            var index = SwipeIndex;
            UpdateSwipeIndex();
            if (index >= NumberOfSlides)
                return;

            var astronomy = (Details)index;
            var data = GetData(astronomy);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var iconPath = GetIconPath(astronomy);
            if (string.IsNullOrWhiteSpace(iconPath))
                return;

            await DrawKeyImageWithIcon(true, Settings.City, data, iconPath);
            Settings.LastSwipe = DateTime.Now;
            await SaveSettings();
        }

        private async Task Redraw()
        {
            if (Settings.Data == null)
                return;

            var index = SwipeIndex - 1;
            if (index < 0)
                index = 0;

            var astronomy = (Details)index;
            var data = GetData(astronomy);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var iconPath = GetIconPath(astronomy);
            if (string.IsNullOrWhiteSpace(iconPath))
                return;

            await DrawKeyImageWithIcon(true, Settings.City, data, iconPath);
        }

        private string GetData(Details details)
        {
            if (Settings.Data == null)
                return null;

            switch (details)
            {
                case Details.Condition: return GetConditionData();
                case Details.FeelsLike: return GetFeelsLikeDataData();
                case Details.Wind: return GetWindDataData();
                case Details.Clouds: return $"{Settings.Data.Current.Cloud} %";
                case Details.Humidity: return $"{Settings.Data.Current.Humidity} %";
                case Details.Uv: return Settings.Data.Current.Uv.ToString(CultureInfo.InvariantCulture);
                default: return null;
            }
        }

        private string GetIconPath(Details details)
        {
            if (Settings.Data == null)
                return null;

            switch (details)
            {
                case Details.Condition: return GetConditonIconPath(Settings.Data);
                case Details.FeelsLike: return GetDetailsIconPath("feelslike");
                case Details.Wind: return GetDetailsIconPath(Settings.Data.Current.WindDir.ToLowerInvariant());
                case Details.Clouds: return GetDetailsIconPath("cloud");
                case Details.Humidity: return GetDetailsIconPath("humidity");
                case Details.Uv: return GetDetailsIconPath("uv");
                default: return null;
            }
        }

        private string GetConditionData()
        {
            return !string.IsNullOrWhiteSpace(Settings.TemperatureUnit) && Settings.TemperatureUnit == "f"
                ? $"{Math.Round(Settings.Data.Current.TempF, 0)} °F"
                : $"{Math.Round(Settings.Data.Current.TempC, 0)} °C";
        }

        private string GetFeelsLikeDataData()
        {
            return !string.IsNullOrWhiteSpace(Settings.TemperatureUnit) && Settings.TemperatureUnit == "f"
                ? $"{Math.Round(Settings.Data.Current.FeelslikeF, 1)} °F"
                : $"{Math.Round(Settings.Data.Current.FeelslikeC, 1)} °C";
        }

        private string GetWindDataData()
        {
            return !string.IsNullOrWhiteSpace(Settings.SpeedUnit) && Settings.SpeedUnit == "mph"
                ? $"{Math.Round(Settings.Data.Current.WindMph, 1)} mph"
                : $"{Math.Round(Settings.Data.Current.WindKph, 1)} km/h";
        }

        private string GetDetailsIconPath(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
                return null;

            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
                return null;

            var currentPath = Path.GetDirectoryName(assemblyLocation);


            return $"{currentPath}\\Images\\details\\{iconName}.png";
        }

        private async Task ShouldLoadData()
        {
            if ((DateTime.Now - Settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(Settings.City))
            {
                try
                {
                    var data = await WeatherApiClient.GetCurrentWeatherData(GlobalSettings.ApiKey, Settings.City);
                    if (data != null)
                    {
                        Settings.Data = data;
                        Settings.LastRefresh = DateTime.Now;
                        await SaveSettings();
                    }
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                }
            }
        }

        private void UpdateSwipeIndex()
        {
            if (string.IsNullOrWhiteSpace(Settings?.City))
                return;

            if (Settings.Data == null)
                return;

            Interlocked.Increment(ref SwipeIndex);
            if (SwipeIndex >= NumberOfSlides)
                Interlocked.Exchange(ref SwipeIndex, 0);
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
                    Settings.LastSwipe = DateTime.Now;
                    await SaveSettings();
                    await Redraw();
                }
            }
        }
    }
}