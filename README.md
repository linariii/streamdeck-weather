# Weather plugin for Elgato Stream Deck

[![CI](https://github.com/linariii/streamdeck-weather/actions/workflows/CI.yml/badge.svg)](https://github.com/linariii/streamdeck-weather/actions/workflows/CI.yml) [![CD](https://github.com/linariii/streamdeck-weather/actions/workflows/CD.yml/badge.svg)](https://github.com/linariii/streamdeck-weather/actions/workflows/CD.yml)

## Actions
* Weather
	* Display the current weather conditons for a selected city
	* Supports celsius and fahrenheit (°C and °F)
	* City name can be hidden
	* Requires an WeatherAPI api key
	* data is refresh every 15 minutes
	* button push > no action
* Weather slider
	* Displays the current weather conditions for multiple cities
	* City names must be comma separated (e.g.: London,Berlin,Paris)
	* Supports celsius and fahrenheit (°C and °F)
	* Requires an WeatherAPI api key
	* data is refresh every 15 minutes
	* every 15 seconds the next entry is displayed
	* button push > display next entry
* Astronomy
	* Display astronomy information (sunrise, sunset, moonrise, moonset, moon phase) for a selected city
	* Requires an WeatherAPI api key
	* data is refresh every 15 minutes
	* every 15 seconds the next entry is displayed
	* button push > display next entry
* Weather forecast
	* Displays the weather forecast for a selected city
	* Supports celsius and fahrenheit (°C and °F)
	* Requires an WeatherAPI api key
	* data is refresh every 15 minutes
	* every 15 seconds the next entry is displayed
	* button push > display next entry
* Weather details
	* displays detailed information (condition, feels like, wind direction, clouds, humidity, uv) for a selected city
	* Supports celsius and fahrenheit (°C and °F)
	* Supports kmh and mph
	* Requires an WeatherAPI api key
	* data is refresh every 15 minutes
	* every 15 seconds the next entry is displayed
	* button push > display next entry

## Support
 - Supports Windows: Yes
 - Supports Mac: No
 
## Dependencies
* Uses [StreamDeck-Tools](https://github.com/BarRaider/streamdeck-tools) by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider
* Uses [WeatherAPI.com](https://www.weatherapi.com/)
