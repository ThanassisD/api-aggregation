using ApiAggregation.Application.Interfaces;
using ApiAggregation.Domain.Entities;
using ApiAggregation.Domain.Enums;
using ApiAggregation.Infrastructure.Clients.ClientModels;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ApiAggregation.Infrastructure.Clients
{
    public class WeatherApiClient : IWeatherApiClient
    {
        private readonly HttpClient      _http;
        private readonly IMemoryCache    _cache;
        private readonly ILogger<WeatherApiClient> _logger;
        private readonly TimeSpan        _cacheDuration;
        private readonly string          _apiKey;

        private const string CACHE_PREFIX       = "Weather:";
        private const string CITY_NULL_ERROR    = "City name must be provided.";
        private const string NON_EXISTING_WEATHER    = "Weather Not found.";
        private const string FETCH_ERROR_FORMAT = "Error calling weather API";
        private const string JSON_ERROR_FORMAT  = "Error deserializing wether response";

        public WeatherApiClient(
            HttpClient http,
            IMemoryCache cache,
            ILogger<WeatherApiClient> logger,
            IConfiguration config)
        {
            _http   = http   ?? throw new ArgumentNullException(nameof(http));
            _cache  = cache  ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _apiKey = config["ApiKeys:OpenWeatherKey"]
                   ?? "DummyApiKey";

            var minutes = config.GetValue<int>("Common:CacheDurationInMinutes", 60);
            _cacheDuration = TimeSpan.FromMinutes(minutes);
            _logger.LogDebug("Weather cache duration: {Minutes} minutes", minutes);
        }

        public async Task<ApiResponseWrapper> GetWeatherAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                _logger.LogWarning("GetWeatherAsync called with null or empty cityName");
                return new ApiResponseWrapper(CITY_NULL_ERROR, ResponseStatus.Error.GetStatus());
            }
            
            var cacheKey = CACHE_PREFIX + cityName.Trim().ToLowerInvariant();

            var cityWeatherMatch = await GetWeatherFromApiAsync(cityName, cacheKey);
            if (cityWeatherMatch.Weather == null || cityWeatherMatch.Weather.Count == 0)
            {
                _logger.LogInformation($"Weather for city '{cityName}' not found");
                _cache.Remove(cacheKey);
                return new ApiResponseWrapper(NON_EXISTING_WEATHER, ResponseStatus.Error.GetStatus());
            }

            var weatherInfo = ToConsumed(cityWeatherMatch);
            
            return new ApiResponseWrapper(
                message: "Success",
                status:  "OK",
                data:    weatherInfo
            );
        }

        private async Task<RestWeatherClientModel> GetWeatherFromApiAsync(string cityName, string cacheKey)
        {
            var emptyResult = new RestWeatherClientModel();
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                    try
                    {
                        _logger.LogDebug("Fetching weather for {City}", cityName);
                        entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

                        var url = $"weather?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
                        using var resp = await _http.GetAsync(url);
                        if (!resp.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Weather API returned status code {StatusCode} for city '{City}'",
                                resp.StatusCode, cityName);
                            return emptyResult;
                        }
                        var json = await resp.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<RestWeatherClientModel>(json);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, FETCH_ERROR_FORMAT);
                        return emptyResult;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, JSON_ERROR_FORMAT);
                        return emptyResult;
                    }
                }) ?? emptyResult;
        }

        private static WeatherInfoModel ToConsumed(RestWeatherClientModel countryMatch)
        {
            if (countryMatch?.Main == null || countryMatch.Weather == null)
            {
                return new WeatherInfoModel(temperature: 0, weather: NON_EXISTING_WEATHER);
            }
        
            return new WeatherInfoModel
            (
                temperature: countryMatch.Main.Temp,
                weather: countryMatch.Weather.FirstOrDefault()?.Description ?? NON_EXISTING_WEATHER
            );
        }
    }
}
