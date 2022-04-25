using System;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weather.Backend;
using Weather.Backend.Models;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.current")]
    public class CurrentWeather : ActionBase
    {
        private const int FetchCooldownSec = 900; // 15 min
        private readonly PluginSettings _settings;

        private class PluginSettings
        {
            [JsonProperty("city")]
            public string City { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("unit")]
            public string Unit { get; set; }

            [JsonProperty("data")]
            public CurrentWeatherResult Data { get; set; }

            [JsonProperty(PropertyName = "lastRefresh")]
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
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                _settings = payload.Settings.ToObject<PluginSettings>();
                if (_settings != null)
                    _settings.LastRefresh = DateTime.MinValue;
            }
        }

        public override async void OnTick()
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (string.IsNullOrWhiteSpace(_globalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(_settings.City))
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
                    await LoadData();
                
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        private async Task LoadData()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(_globalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(_settings.City))
            {
                try
                {
                    var data = await WeatherApiClient.GetCurrentWeatherData(_globalSettings.ApiKey, _settings.City);
                    if (data != null)
                    {
                        _settings.Data = data;
                        _settings.LastRefresh = DateTime.Now;
                        await SaveSettings();
                        await DrawKeyImage();
                    }
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                }
            }
        }

        private async Task DrawKeyImage()
        {
            var showTitle = !string.IsNullOrWhiteSpace(_settings.DisplayName) && _settings.DisplayName == "1";
            var title = !string.IsNullOrWhiteSpace(_settings.Data.Location?.Name)
                ? _settings.Data.Location?.Name
                : _settings.City;

            var data = !string.IsNullOrWhiteSpace(_settings.Unit) && _settings.Unit == "f"
                ? $"{Math.Round(_settings.Data.Current.TempF, 0)} °F"
                : $"{Math.Round(_settings.Data.Current.TempC, 0)} °C";

            var iconPath = GetConditionIconPath(_settings.Data);

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
                if (Tools.AutoPopulateSettings(_settings, payload.Settings) > 0)
                {
                    _settings.LastRefresh = DateTime.MinValue;
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