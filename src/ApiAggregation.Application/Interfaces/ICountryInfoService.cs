using ApiAggregation.Domain.Entities;

namespace ApiAggregation.Application.Interfaces;

public interface ICountryInfoService
{
    Task<ApiResponseWrapper> GetCapitalCityAsync(string countryName);
}