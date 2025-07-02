using ApiAggregation.Domain.Entities;

namespace ApiAggregation.Application.Interfaces;

public interface INewsApiClient
{
    Task<IEnumerable<ApiResponseWrapper>> GetTopHeadlinesAsync(string query, string fromDate ,int pageSize);
}