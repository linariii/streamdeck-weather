using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weather.Backend;
using Weather.Backend.Models;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.multi.weather")]
    public class MultiWeather : ActionBase
    {
        private const int FetchCooldownSec = 300; // 5 min
        private const int SwipeCooldownSec = 30;
        private readonly PluginSettings _settings;
        private int _swipeIndex = 0;

        private class PluginSettings
        {
            [JsonProperty("cities")]
            public string Cities { get; set; }

            [JsonProperty("data")]
            public List<CurrentWeatherResult> Data { get; set; }

            [JsonProperty(PropertyName = "lastRefresh")]
            public DateTime LastRefresh { get; set; }

            [JsonProperty(PropertyName = "lastSwipe")]
            public DateTime LastSwipe { get; set; }

            [JsonProperty("unit")]
            public string Unit { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                return new PluginSettings
                {
                    LastRefresh = DateTime.MinValue,
                    LastSwipe = DateTime.MinValue
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
                if (_settings != null)
                {
                    _settings.LastRefresh = DateTime.MinValue;
                    _settings.LastSwipe = DateTime.MinValue;
                }
            }
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (string.IsNullOrWhiteSpace(_globalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(_settings.Cities))
                return;

            if (_settings.Data == null || !_settings.Data.Any())
                return;

            if (_isRunning > 0)
                return;

            var locked = false;
            try
            {
                try { }
                finally
                {
                    locked = Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0;
                }

                if (locked)
                {
                    await DrawNext();
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        public override async void OnTick()
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (string.IsNullOrWhiteSpace(_globalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(_settings.Cities))
                return;

            if (_isRunning > 0)
                return;

            var locked = false;
            try
            {
                try { }
                finally
                {
                    locked = Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0;
                }

                if (locked)
                {
                    await ShouldLoadData();
                    if (_settings.Data != null && _settings.Data.Any())
                    {
                        await ShouldDrawNext();
                    }
                }

            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        private async Task ShouldDrawNext()
        {
            if ((DateTime.Now - _settings.LastSwipe).TotalSeconds > SwipeCooldownSec)
            {
                await DrawNext();
            }
        }

        private async Task DrawNext()
        {
            var index = _swipeIndex;
            UpdateSwipeIndex();
            if (index >= _settings.Data.Count)
                return;

            var data = _settings.Data[index];
            if (data == null)
                return;

            var title = !string.IsNullOrWhiteSpace(data.Location?.Name)
                ? data.Location?.Name
                : "";

            var tempStr = !string.IsNullOrWhiteSpace(_settings.Unit) && _settings.Unit == "f"
                ? $"{Math.Round(data.Current.TempF, 0)} °F"
                : $"{Math.Round(data.Current.TempC, 0)} °C";

            var iconPath = GetConditonIconPath(data);

            await DrawWeatherKeyImage(!string.IsNullOrWhiteSpace(title), title, tempStr, iconPath);
            _settings.LastSwipe = DateTime.Now;
            await SaveSettings();
        }

        private async Task ShouldLoadData()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(_globalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(_settings.Cities))
            {
                try
                {
                    var results = new List<CurrentWeatherResult>();
                    foreach (var cityName in _settings.Cities.Split(','))
                    {
                        var result = await WeatherApiClient.GetCurrentWeatherData(_globalSettings.ApiKey, cityName.Trim());
                        if (result != null)
                            results.Add(result);
                    }

                    if (results.Any())
                    {
                        _settings.Data = results;
                        _settings.LastRefresh = DateTime.Now;
                        _settings.LastSwipe = DateTime.MinValue;
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

        public override void KeyReleased(KeyPayload payload) { }

        private void UpdateSwipeIndex()
        {
            if (string.IsNullOrWhiteSpace(_settings?.Cities))
                return;

            if (_settings.Data.Count == 0)
                return;

            Interlocked.Increment(ref _swipeIndex);
            if (_swipeIndex >= _settings.Data.Count)
                Interlocked.Exchange(ref _swipeIndex, 0);
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
                if (Tools.AutoPopulateSettings(_settings, payload.Settings) > 0)
                {
                    _settings.LastRefresh = DateTime.MinValue;
                    _settings.LastSwipe = DateTime.Now;
                    await SaveSettings();
                }
            }
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