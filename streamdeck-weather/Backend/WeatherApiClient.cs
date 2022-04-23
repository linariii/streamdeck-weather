using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Weather.Backend.Models;

namespace Weather.Backend
{
    public class WeatherApiClient
    {
        private const string BaseUrl = "https://api.weatherapi.com/v1";
        public static async Task<CurrentWeatherResult> GetCurrentWeatherData(string apiKey, string cityName)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cityName))
                return null;

            var result = await DownloadString($"{BaseUrl}/current.json?key={apiKey}&q={cityName}&aqi=no");
            return string.IsNullOrWhiteSpace(result)
                ? null
                : JsonConvert.DeserializeObject<CurrentWeatherResult>(result);
        }

        public static async Task<AstronomyResult> GetAstronomyData(string apiKey, string cityName)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cityName))
                return null;

            var result = await DownloadString($"{BaseUrl}/astronomy.json?key={apiKey}&q={cityName}&aqi=no");
            return string.IsNullOrWhiteSpace(result)
                ? null
                : JsonConvert.DeserializeObject<AstronomyResult>(result);
        }

        public static async Task<ForecastResult> GetForeCastData(string apiKey, string cityName, int days = 3)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cityName))
                return null;

            var result = await DownloadString($"{BaseUrl}/forecast.json?key={apiKey}&q={cityName}&aqi=no&days={days}");
            return string.IsNullOrWhiteSpace(result)
                ? null
                : JsonConvert.DeserializeObject<ForecastResult>(result);
        }

        public static async Task<List<CurrentWeatherResult>> GetCurrentWeatherData(string apiKey, params string[] cityNames)
        {
            var results = new List<CurrentWeatherResult>();
            foreach (var cityName in cityNames)
            {
                var result = await GetCurrentWeatherData(apiKey, cityName);
                if (result != null)
                    results.Add(result);
            }

            return results;
        }

        private static async Task<string> DownloadString(string url)
        {
            using (var client = new System.Net.WebClient())
            {
                return await client.DownloadStringTaskAsync(url);
            }
        }
    }
}