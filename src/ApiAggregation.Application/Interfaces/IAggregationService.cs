using ApiAggregation.Domain.Entities;

namespace ApiAggregation.Application.Interfaces;

public interface IAggregationService
{
    Task<AggregatedResponse> GetAggregatedDataAsync(string countryName, int newsPageSize, string fromDate);
}