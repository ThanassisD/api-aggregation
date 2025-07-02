using System.Reflection;
using ApiAggregation.Application.Interfaces;
using ApiAggregation.Domain.Entities;
using ApiAggregation.Domain.Enums;

namespace ApiAggregation.Application.Services;

public class AggregationService : IAggregationService
{
    private readonly ICountryInfoService _countryInfo;
    private readonly IWeatherApiClient   _weatherApi;
    private readonly INewsApiClient      _newsApi;
    
    private const string CONTRY_NOT_FOUND = "Country not found";
    private const string COUNTTRY_NOT_EMPTY = "Country name cannot be empty";
    
    
    public AggregationService(ICountryInfoService countryInfo, IWeatherApiClient weatherApi, INewsApiClient newsApi)
    {
        _countryInfo = countryInfo;
        _weatherApi = weatherApi;
        _newsApi = newsApi;
    }
    
    public async Task<AggregatedResponse> GetAggregatedDataAsync(string countryName, int newsPageSize, string fromDate)
    {
        var apiResponses = new List<ApiResponseWrapper>();
        
        if(string.IsNullOrWhiteSpace(countryName))
            return CreateErrorResponse("BadRequest", COUNTTRY_NOT_EMPTY);
        
        newsPageSize = newsPageSize <= 0 ? 10 : newsPageSize; 
        fromDate = string.IsNullOrWhiteSpace( fromDate) 
            ? DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd") 
            : fromDate;
        
        var countryInfo = await _countryInfo.GetCapitalCityAsync(countryName);
        if (countryInfo.Data == null)
            return CreateErrorResponse("NotFound",  CONTRY_NOT_FOUND);
        apiResponses.Add(countryInfo);
        
        var countryCapital = GetCountryCapital(countryInfo);
        var weatherTask = _weatherApi.GetWeatherAsync(countryCapital);
        var newsTask = _newsApi.GetTopHeadlinesAsync(countryName,fromDate ,newsPageSize);
        await Task.WhenAll(weatherTask, newsTask);
        
        var weather = weatherTask.Result;
        var news = newsTask.Result;
        
        apiResponses.Add(weather);
        apiResponses.AddRange(news);
        
        var firstError  = apiResponses.FirstOrDefault(x=> x.Status == ResponseStatus.Error.GetStatus());
        
        return new AggregatedResponse(
            apiResponses,
            firstError is null ? ResponseStatus.Success.GetStatus() : ResponseStatus.Error.GetStatus(),
            firstError is null ? "All data aggregated successfully." : "Some errors occurred. Check internal messages for details."
        );
    }
    
    private string GetCountryCapital(ApiResponseWrapper countryInfo)
    {
        var capObj    = countryInfo.Data;
        var capProp   = capObj?.GetType().GetProperty("CapitalCity", BindingFlags.Public | BindingFlags.Instance);
        var capital   = capProp?.GetValue(capObj) as string
                        ?? capObj as string; 
        return capital;
    }
    
    private AggregatedResponse CreateErrorResponse(string status,  string apiMessage)
    {
        return new AggregatedResponse(
            new List<ApiResponseWrapper>
            {
                new(apiMessage, status)
            },
            status,
            "An error occurred while processing your request. See internal messages for details."
        );
    }
}