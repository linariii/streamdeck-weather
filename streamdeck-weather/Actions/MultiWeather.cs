using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Weather.Backend.Models;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.multi.weather")]
    public class MultiWeather : ActionBase
    {
        private DateTime _lastSwipe = DateTime.Now;
        public MultiWeather(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = MultiWeatherPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<MultiWeatherPluginSettings>();
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        protected MultiWeatherPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as MultiWeatherPluginSettings;
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

            if (string.IsNullOrWhiteSpace(Settings.Cities))
                return;

            if (Settings.Data == null || !Settings.Data.Any())
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

            if (string.IsNullOrWhiteSpace(Settings.Cities))
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
                    if (Settings.Data != null && Settings.Data.Any())
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
            else if ((DateTime.Now - _lastSwipe).TotalSeconds > SwipeCooldownSec)
            {
                await DrawNext();
            }
        }

        private async Task DrawNext()
        {
            if (Settings.Data == null || !Settings.Data.Any())
                return;

            IsInitialized = true;
            var index = SwipeIndex;
            UpdateSwipeIndex();
            if (index >= Settings.Data.Count)
                return;

            var data = Settings.Data[index];
            if (data == null)
                return;

            var title = !string.IsNullOrWhiteSpace(data.Location?.Name)
                ? data.Location?.Name
                : "";

            var tempStr = !string.IsNullOrWhiteSpace(Settings.Unit) && Settings.Unit == "f"
                ? $"{Math.Round(data.Current.TempF, 0)} °F"
                : $"{Math.Round(data.Current.TempC, 0)} °C";

            var iconPath = GetConditonIconPath(data);

            await DrawKeyImageWithIcon(!string.IsNullOrWhiteSpace(title), title, tempStr, iconPath);
            _lastSwipe = DateTime.Now;
        }

        private async Task Redraw()
        {
            if (Settings.Data == null || !Settings.Data.Any())
                return;

            var index = GetPreviousSwipeIndex();
            if (index < 0 || index >= Settings.Data.Count)
                index = 0;

            var data = Settings.Data[index];
            if (data == null)
                return;

            var title = !string.IsNullOrWhiteSpace(data.Location?.Name)
                ? data.Location?.Name
                : "";

            var tempStr = !string.IsNullOrWhiteSpace(Settings.Unit) && Settings.Unit == "f"
                ? $"{Math.Round(data.Current.TempF, 0)} °F"
                : $"{Math.Round(data.Current.TempC, 0)} °C";

            var iconPath = GetConditonIconPath(data);

            await DrawKeyImageWithIcon(!string.IsNullOrWhiteSpace(title), title, tempStr, iconPath);
        }

        private int GetPreviousSwipeIndex()
        {
            var index = SwipeIndex;
            if (index == 0)
                return Settings.Data.Count - 1;

            index--;
            if (index < 0)
                index = 0;

            return index;
        }

        private async Task ShouldLoadData()
        {
            if ((DateTime.Now - Settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(Settings.Cities))
            {
                try
                {
                    var results = new List<CurrentWeatherResult>();
                    foreach (var cityName in Settings.Cities.Split(','))
                    {
                        var result = await LoadWeatherData(cityName.Trim());
                        if (result != null)
                            results.Add(result);
                    }

                    if (results.Any())
                    {
                        Settings.Data = results;
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
            if (string.IsNullOrWhiteSpace(Settings?.Cities))
                return;

            if (Settings.Data == null || !Settings.Data.Any())
                return;

            Interlocked.Increment(ref SwipeIndex);
            if (SwipeIndex >= Settings.Data.Count)
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
                    _lastSwipe = DateTime.Now;
                    await SaveSettings();
                    await Redraw();
                }
            }
        }
    }
}