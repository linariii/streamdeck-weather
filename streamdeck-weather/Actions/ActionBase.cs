using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using Weather.Backend;
using Weather.Backend.Models;
using Weather.Settings;

namespace Weather.Actions
{
    public abstract class ActionBase : PluginBase
    {
        private protected const int FetchCooldownSec = 900; // 15 min
        private protected readonly GlobalSettings GlobalSettings;
        private protected int IsRunning = 0;
        private protected PluginSettingsBase BaseSettings;
        private protected const int SwipeCooldownSec = 15;
        private protected int SwipeIndex = 0;
        private protected bool IsInitialized = false;

        protected ActionBase(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            GlobalSettings = GlobalSettings.CreateDefaultSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, "ReceivedGlobalSettings");
#endif
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings: {payload.Settings}");
#endif
                var settings = payload.Settings.ToObject<GlobalSettings>();
                if (settings != null && GlobalSettings != null)
                {
                    var updated = false;
                    if (settings.ApiKey != GlobalSettings.ApiKey)
                    {
                        GlobalSettings.ApiKey = settings.ApiKey;
                        updated = true;
                    }

                    await SaveGlobalSettings(updated);
                }
            }
        }

        private async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (GlobalSettings != null)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(GlobalSettings)}");
#endif
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(GlobalSettings), triggerDidReceiveGlobalSettings);
            }
        }

        private protected async Task SaveSettings()
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(BaseSettings)}");
#endif
            await Connection.SetSettingsAsync(JObject.FromObject(BaseSettings));
        }

        public override void Dispose() { }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        private protected string GetConditonIconPath(CurrentWeatherResult data)
        {
            if (string.IsNullOrWhiteSpace(data?.Current?.Condition?.Icon))
                return null;

            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
                return null;

            var currentPath = Path.GetDirectoryName(assemblyLocation);

            var index = data.Current.Condition.Icon.IndexOf("/weather/", StringComparison.Ordinal);
            var iconSubPath = data.Current.Condition.Icon.Substring(index).Replace("/", "\\");

            return $"{currentPath}\\Images{iconSubPath}";
        }

        private protected async Task DrawKeyImageWithIcon(bool showTitle, string title, string data, string iconPath)
        {
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(iconPath))
                return;

            if (showTitle && string.IsNullOrWhiteSpace(title))
                return;

            try
            {
                using (var bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    var width = bmp.Width;

                    var fgBrush = new SolidBrush(Color.White);
                    float stringWidth;

                    // Top title
                    if (showTitle)
                    {
                        var fontTitle = new Font("Verdana", 20, FontStyle.Bold, GraphicsUnit.Pixel);
                        stringWidth = graphics.GetTextCenter(title, width, fontTitle);
                        var fontSizeDefault = graphics.GetFontSizeWhereTextFitsImage(title, width, fontTitle, 8);
                        fontTitle = new Font(fontTitle.Name, fontSizeDefault, fontTitle.Style, GraphicsUnit.Pixel);
                        graphics.DrawAndMeasureString(title, fontTitle, fgBrush, new PointF(stringWidth, 5));
                        fontTitle.Dispose();
                    }

                    // Background
                    if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                    {
                        var size = showTitle ? 60 : 85;
                        var backgroundImagePosY = showTitle ? 37 : 5;
                        var backgroundImagePosX = (width - size) / 2;
                        var backgroundImage = Image.FromFile(iconPath);
                        graphics.DrawImage(backgroundImage, backgroundImagePosX, backgroundImagePosY, size, size);
                        backgroundImage.Dispose();
                    }

                    //temp
                    var fontTemp = new Font("Verdana", 28, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontSizeTemp = graphics.GetFontSizeWhereTextFitsImage(data, width, fontTemp, 20);
                    fontTemp = new Font(fontTemp.Name, fontSizeTemp, fontTemp.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(data, width, fontTemp);
                    var tempPos = showTitle ? 106 : 105;
                    graphics.DrawAndMeasureString(data, fontTemp, fgBrush, new PointF(stringWidth, tempPos));

                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontTemp.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} Error drawing data {ex}");
            }
        }

        private protected virtual async Task<CurrentWeatherResult> ShouldLoadWeatherData(string city)
        {
            if ((DateTime.Now - BaseSettings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.ApiKey)
                && !string.IsNullOrWhiteSpace(city))
            {
                return await LoadWeatherData(city);
            }
            return null;
        }

        private protected async Task<CurrentWeatherResult> LoadWeatherData(string city)
        {
            if (!string.IsNullOrWhiteSpace(GlobalSettings.ApiKey) && !string.IsNullOrWhiteSpace(city))
            {
                try
                {
                    return await WeatherApiClient.GetCurrentWeatherData(GlobalSettings.ApiKey, city);

                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                }
            }
            return null;
        }
    }
}