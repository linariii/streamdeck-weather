using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Weather.Backend;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.astronomy")]
    public class Astronomy : ActionBase
    {
        private readonly int _numberOfSlides = Enum.GetNames(typeof(Enums.Astronomy)).Length;
        public Astronomy(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = AstronomyPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<AstronomyPluginSettings>();
                if (Settings != null)
                    Settings.LastSwipe = DateTime.Now;
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        protected AstronomyPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as AstronomyPluginSettings;
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
            if (index >= _numberOfSlides)
                return;

            var astronomy = (Enums.Astronomy)index;
            var data = GetData(astronomy);

            if (string.IsNullOrWhiteSpace(data))
                return;

            var iconName = GetIconPath(astronomy);
            if (string.IsNullOrWhiteSpace(iconName))
                return;

            var iconPath = GetAstronomyIconPath(iconName);
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

            var astronomy = (Enums.Astronomy)index;
            var data = GetData(astronomy);

            if (string.IsNullOrWhiteSpace(data))
                return;

            var iconName = GetIconPath(astronomy);
            if (string.IsNullOrWhiteSpace(iconName))
                return;

            var iconPath = GetAstronomyIconPath(iconName);
            await DrawKeyImageWithIcon(true, Settings.City, data, iconPath);
        }

        private string GetData(Enums.Astronomy astronomy)
        {
            if (Settings.Data == null)
                return null;

            switch (astronomy)
            {
                case Enums.Astronomy.Sunrise:
                    return Settings.Data.Astronomy.Astro.Sunrise;

                case Enums.Astronomy.Sunset:
                    return Settings.Data.Astronomy.Astro.Sunset;

                case Enums.Astronomy.Moonrise:
                    return Settings.Data.Astronomy.Astro.Moonrise;

                case Enums.Astronomy.Moonset:
                    return Settings.Data.Astronomy.Astro.Moonset;

                case Enums.Astronomy.Moonphase:
                    return Settings.Data.Astronomy.Astro.MoonPhase;

                default:
                    return null;
            }
        }

        private string GetIconPath(Enums.Astronomy astronomy)
        {
            if (Settings.Data == null)
                return null;

            switch (astronomy)
            {
                case Enums.Astronomy.Sunrise:
                    return "sunrise.png";

                case Enums.Astronomy.Sunset:
                    return "sunset.png";

                case Enums.Astronomy.Moonrise:
                    return "moonrise.png";

                case Enums.Astronomy.Moonset:
                    return "moonset.png";

                case Enums.Astronomy.Moonphase:
                    return $"{Settings.Data.Astronomy.Astro.MoonPhase.ToLowerInvariant()}.png";

                default:
                    return null;
            }
        }

        private string GetAstronomyIconPath(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
                return null;

            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
                return null;

            var currentPath = Path.GetDirectoryName(assemblyLocation);


            return $"{currentPath}\\Images\\astronomy\\{iconName}";
        }

        private async Task ShouldLoadData()
        {
            if ((DateTime.Now - Settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(Settings.City))
            {
                try
                {
                    var data = await WeatherApiClient.GetAstronomyData(GlobalSettings.ApiKey, Settings.City);
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
            if (SwipeIndex >= _numberOfSlides)
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