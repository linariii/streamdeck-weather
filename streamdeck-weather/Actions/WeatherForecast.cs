using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Weather.Backend;
using Weather.Backend.Models;
using Weather.Settings;

namespace Weather.Actions
{
    [PluginActionId("com.linariii.weather.forecast")]
    public class WeatherForecast : ActionBase
    {
        private DateTime _lastSwipe = DateTime.Now;
        private StringBuilder _scrollingTitle;
        private string _temperatureText;
        private string _iconPath;

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

        public override async void KeyPressed(KeyPayload payload)
        {
            if (GlobalSettings == null || Settings == null)
                return;

            if (string.IsNullOrWhiteSpace(GlobalSettings.ApiKey))
                return;

            if (string.IsNullOrWhiteSpace(Settings.City))
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
            else
            {
                await UpdateTitle();
            }
        }

        private async Task UpdateTitle()
        {
            await DrawKeyImage();
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

            SetScrollingTitle(data);
            SetTemperatureText(data);
            SetIconPath(data);

            await DrawKeyImage();
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

            SetScrollingTitle(data);
            SetTemperatureText(data);
            SetIconPath(data);

            await DrawKeyImage();
        }

        private void SetTemperatureText(ForecastDay data)
        {
            _temperatureText = !string.IsNullOrWhiteSpace(Settings.Unit) && Settings.Unit == "f"
                ? $"{Math.Round(data.Day.AvgTempF, 0)} °F"
                : $"{Math.Round(data.Day.AvgTempC, 0)} °C";
        }

        private void SetIconPath(ForecastDay data)
        {
            _iconPath = GetConditonIconPath(data.Day.Condition);
        }

        private void SetScrollingTitle(ForecastDay data)
        {
            _scrollingTitle = DateTime.TryParse(data.Date, out var date) 
                ? new StringBuilder($"{Settings.City} - {date:M}    ") 
                : new StringBuilder($"{Settings.City} - {data.Date}    ");
        }

        private async Task DrawKeyImage()
        {
            if (string.IsNullOrWhiteSpace(_iconPath) || string.IsNullOrWhiteSpace(_temperatureText) || _scrollingTitle == null)
                return;

            try
            {
                using (var bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    var width = bmp.Width;
                    var fgBrush = new SolidBrush(Color.White);

                    // Top title
                    var title = _scrollingTitle.ToString();
                    var str = _scrollingTitle.ToString().Substring(0, 2);
                    _scrollingTitle = _scrollingTitle.Remove(0, 2).Append(str);
                    var fontTitle = new Font("Verdana", 20, FontStyle.Bold, GraphicsUnit.Pixel);
                    graphics.DrawString(title, fontTitle, fgBrush, new PointF(5, 5));
                    fontTitle.Dispose();

                    // Background
                    if (!string.IsNullOrWhiteSpace(_iconPath) && File.Exists(_iconPath))
                    {
                        var size = 60;
                        var backgroundImagePosY = 37;
                        var backgroundImagePosX = (width - size) / 2;
                        var backgroundImage = Image.FromFile(_iconPath);
                        graphics.DrawImage(backgroundImage, backgroundImagePosX, backgroundImagePosY, size, size);
                        backgroundImage.Dispose();
                    }

                    //temp
                    var fontTemp = new Font("Verdana", 28, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontSizeTemp = graphics.GetFontSizeWhereTextFitsImage(_temperatureText, width, fontTemp, 20);
                    fontTemp = new Font(fontTemp.Name, fontSizeTemp, fontTemp.Style, GraphicsUnit.Pixel);
                    var stringWidth = graphics.GetTextCenter(_temperatureText, width, fontTemp);
                    var tempPos = 106;
                    graphics.DrawAndMeasureString(_temperatureText, fontTemp, fgBrush, new PointF(stringWidth, tempPos));

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
                && !string.IsNullOrWhiteSpace(Settings.City))
            {
                try
                {
                    var data = await WeatherApiClient.GetForeCastData(GlobalSettings.ApiKey, Settings.City);

                    if (data?.Forecast != null && data.Forecast.Forecastday.Any())
                    {
                        Settings.Data = data.Forecast.Forecastday;
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