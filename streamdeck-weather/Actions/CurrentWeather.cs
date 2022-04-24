using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weather.Backend;
using Weather.Backend.Models;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.current")]
    public class CurrentWeather : PluginBase
    {
        private const int FetchCooldownSec = 900; // 15 min
        private readonly PluginSettings _settings;
        private readonly GlobalSettings _globalSettings;

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
                _globalSettings = GlobalSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
                _globalSettings = GlobalSettings.CreateDefaultSettings();
                _settings = payload.Settings.ToObject<PluginSettings>();
                if (_settings != null)
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

        public override async void OnTick()
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (string.IsNullOrWhiteSpace(_globalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(_settings.City))
                return;

            await LoadData();
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
                        await Draw();
                    }
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                }
            }
        }

        private async Task Draw()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, GetIconPath());
            try
            {
                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    var height = bmp.Height;
                    var width = bmp.Width;

                    var fontDefault = new Font("Verdana", 20, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontCurrency = new Font("Verdana", 28, FontStyle.Bold, GraphicsUnit.Pixel);

                    var fgBrush = new SolidBrush(Color.White);
                    var stringWidth = graphics.GetTextCenter(_settings.City, width, fontDefault);
                    var showTitle = !string.IsNullOrWhiteSpace(_settings.DisplayName) && _settings.DisplayName == "1";

                    // Top title
                    if (showTitle)
                    {
                        var title = !string.IsNullOrWhiteSpace(_settings.Data.Location?.Name)
                            ? _settings.Data.Location?.Name
                            : _settings.City;

                        var fontSizeDefault = graphics.GetFontSizeWhereTextFitsImage(title, width, fontDefault, 8);
                        fontDefault = new Font(fontDefault.Name, fontSizeDefault, fontDefault.Style, GraphicsUnit.Pixel);
                        graphics.DrawAndMeasureString(_settings.City, fontDefault, fgBrush, new PointF(stringWidth, 5));
                    }

                    // Background
                    var iconPath = GetIconPath();
                    if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                    {
                        var backgroundImagePos = showTitle ? 20 : 5;
                        var backgroundImage = Image.FromFile(iconPath);
                        graphics.DrawImage(backgroundImage, 27, backgroundImagePos, 90, 90);
                    }

                    //temp
                    var currStr = !string.IsNullOrWhiteSpace(_settings.Unit) && _settings.Unit == "f"
                        ? $"{Math.Round(_settings.Data.Current.TempF, 0)} °F"
                        : $"{Math.Round(_settings.Data.Current.TempC, 0)} °C";

                    var fontSizeTemp = graphics.GetFontSizeWhereTextFitsImage(currStr, width, fontCurrency, 20);
                    fontCurrency = new Font(fontCurrency.Name, fontSizeTemp, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(currStr, width, fontCurrency);
                    var tempPos = showTitle ? 111 : 105;
                    graphics.DrawAndMeasureString(currStr, fontCurrency, fgBrush, new PointF(stringWidth, tempPos));

                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontDefault.Dispose();
                    fontCurrency.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} Error drawing data {ex}");
            }
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings: {payload.Settings}");
                if (Tools.AutoPopulateSettings(_settings, payload.Settings) > 0)
                {
                    _settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
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

        private string GetIconPath()
        {
            if (string.IsNullOrWhiteSpace(_settings?.Data?.Current?.Condition?.Icon))
                return null;

            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
                return null;

            var currentPath = Path.GetDirectoryName(assemblyLocation);

            var index = _settings.Data.Current.Condition.Icon.IndexOf("/weather/", StringComparison.Ordinal);
            var iconSubPath = _settings.Data.Current.Condition.Icon.Substring(index).Replace("/", "\\");

            return $"{currentPath}{Path.DirectorySeparatorChar}Images{iconSubPath}";
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