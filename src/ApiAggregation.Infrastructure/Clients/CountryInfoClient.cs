using System.Text.Json;
using ApiAggregation.Application.Interfaces;
using ApiAggregation.Domain.Entities;
using ApiAggregation.Domain.Enums;
using ApiAggregation.Infrastructure.Clients.ClientModels;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApiAggregation.Infrastructure.Clients;

public class CountryInfoClient : ICountryInfoService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CountryInfoClient> _logger;
    private readonly TimeSpan _cacheDuration;
    
    private const string ALL_COUNTRIES_CACHE_KEY = "AllCountries";
    private const string COUNTRY_NULL_ERROR = "Country name cannot be null or empty.";
    private const string NO_COUNTRIES_FOUND_ERROR = "No countries found in the external API.";
    private const string COUNTRY_NOT_FOUND_ERROR = "Country not found in the external API.";
    private const string NO_CAPITAL_CITY_FOUND = "No capital city found";

    public CountryInfoClient(
        HttpClient http,
        IMemoryCache cache,
        ILogger<CountryInfoClient> logger,
        IConfiguration configuration)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var cacheDurationMinutes = configuration.GetValue<int>("Common:CacheDurationInMinutes", 60);
        _cacheDuration = TimeSpan.FromMinutes(cacheDurationMinutes);
        _logger.LogDebug("Cache duration set to {CacheDuration} minutes", cacheDurationMinutes);
    }

    public async Task<ApiResponseWrapper> GetCapitalCityAsync(string countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
        {
            _logger.LogWarning("GetCapitalCityAsync called with null or empty country name");
            return new ApiResponseWrapper(COUNTRY_NULL_ERROR, ResponseStatus.Error.GetStatus());
        }
        
        try
        {
            var allCountries = await GetAllCountriesAsync();
            if (allCountries == null || allCountries.Length == 0)
            {
                _logger.LogWarning("No countries retrieved from external API");
                return new ApiResponseWrapper(NO_COUNTRIES_FOUND_ERROR, ResponseStatus.Error.GetStatus());
            }
            var countryMatch = FindCountryByName(allCountries, countryName);
            if (countryMatch is null)
            {
                _logger.LogInformation("Country '{CountryName}' not found", countryName);
                _cache.Remove(ALL_COUNTRIES_CACHE_KEY);
                return new ApiResponseWrapper(COUNTRY_NOT_FOUND_ERROR, ResponseStatus.Error.GetStatus());
            }
            var contryInfo = ToConsumed(countryMatch);
            
            return new ApiResponseWrapper(
                "Success",
                ResponseStatus.Success.GetStatus(),
                contryInfo
            );
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving capital city for {CountryName}", countryName);
            return new ApiResponseWrapper($"Error retrieving country information: {ex.Message}", ResponseStatus.Error.GetStatus());
        }
    }

    private async Task<RestCountryClientModel[]?> GetAllCountriesAsync()
    {
        return await _cache.GetOrCreateAsync(
            ALL_COUNTRIES_CACHE_KEY,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                
                try
                {
                    _logger.LogDebug("Fetching countries from external API");
                    var resp = await _http.GetAsync("all?fields=cca2,name,capital");
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to retrieve countries, status code: {StatusCode}", resp.StatusCode);
                        return Array.Empty<RestCountryClientModel>();
                    }

                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    var list = await JsonSerializer.DeserializeAsync<RestCountryClientModel[]>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    _logger.LogInformation("Retrieved {Count} countries from external API", list?.Length ?? 0);
                    return list ?? Array.Empty<RestCountryClientModel>();
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error calling countries API");
                    return Array.Empty<RestCountryClientModel>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing countries response");
                    return Array.Empty<RestCountryClientModel>();
                }
            });
    }

    private static RestCountryClientModel? FindCountryByName(RestCountryClientModel[] countries, string name)
    {
        var country = countries.FirstOrDefault(c =>
            c.Name.Official.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Common.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (country == null)
        {
            country = countries.FirstOrDefault(c =>
                c.Name.Official.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Common.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
        
        return country;
    }
    
    private static CountryInfoModel ToConsumed(RestCountryClientModel src)
    {
        if (src.Capital is null || src.Capital.Count == 0)
            return new CountryInfoModel(src.Name.Common, NO_CAPITAL_CITY_FOUND);

        return new CountryInfoModel(
            src.Name.Common,
            string.Join(" ", src.Capital)
        );
    }
}