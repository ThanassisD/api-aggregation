using ApiAggregation.Domain.Entities;

namespace ApiAggregation.Application.Interfaces;

public interface IWeatherApiClient
{
    Task<ApiResponseWrapper> GetWeatherAsync(string cityName);
}