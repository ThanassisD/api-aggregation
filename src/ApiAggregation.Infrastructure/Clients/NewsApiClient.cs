using ApiAggregation.Application.Interfaces;
using ApiAggregation.Domain.Entities;
using ApiAggregation.Domain.Enums;
using ApiAggregation.Infrastructure.Clients.ClientModels;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Article = ApiAggregation.Infrastructure.Clients.ClientModels.Article;

namespace ApiAggregation.Infrastructure.Clients
{
    public class NewsApiClient : INewsApiClient
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NewsApiClient> _logger;
        private readonly TimeSpan _cacheDuration;
        private readonly string _apiKey;
        private readonly string _fromDate;

        private const string CACHE_PREFIX = "News:";
        private const string QUERY_NULL_ERROR = "Search query must be provided.";
        private const string NO_NEWS_FOUND = "No news articles found.";
        private const string FETCH_ERROR_FORMAT = "Error calling news API";
        private const string JSON_ERROR_FORMAT = "Error deserializing news response";
        private const string DATE_FORMAT_ERROR = "Please use this date format: yyyy-MM-dd";

        public NewsApiClient(
            HttpClient http,
            IMemoryCache cache,
            ILogger<NewsApiClient> logger,
            IConfiguration config)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _apiKey = config["ApiKeys:NewsApiKey"]
                   ?? "DummyApiKey";
            _fromDate = config.GetValue<string>("ExternalApis:NewsDateFormat" , "yyyy-MM-dd");

            var minutes = config.GetValue<int>("Common:CacheDurationInMinutes", 60);
            _cacheDuration = TimeSpan.FromMinutes(minutes);
            _logger.LogDebug("News cache duration: {Minutes} minutes", minutes);
        }

        public async Task<IEnumerable<ApiResponseWrapper>> GetTopHeadlinesAsync(string query, string fromDate ,int pageSize)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("GetTopHeadlinesAsync called with null or empty query");
                return new[] { new ApiResponseWrapper(QUERY_NULL_ERROR, ResponseStatus.Error.GetStatus()) };
            }
            if (!string.IsNullOrEmpty(fromDate) && !IsValidDateFormat(fromDate))
            {
                _logger.LogWarning("Invalid date format provided: {FromDate}", fromDate);
                return new[] { new ApiResponseWrapper(DATE_FORMAT_ERROR, ResponseStatus.Error.GetStatus()) };
            }

            var cacheKey = $"{CACHE_PREFIX}{query.Trim().ToLowerInvariant()}:{pageSize}:{fromDate}";

            var newsResults = await GetNewsFromApiAsync(query, pageSize, fromDate ,cacheKey);
            if (newsResults?.Articles == null || !newsResults.Articles.Any())
            {
                _logger.LogInformation($"No news found for query '{query}'");
                _cache.Remove(cacheKey);
                return new[] { new ApiResponseWrapper(NO_NEWS_FOUND, ResponseStatus.Error.GetStatus()) };
            }

            var newsInfo = ToConsumed(newsResults);

            return new[] {
                new ApiResponseWrapper(
                    message: "Success",
                    status: ResponseStatus.Success.GetStatus(),
                    data: newsInfo
                )
            };
        }

        private async Task<RestNewsClientModel> GetNewsFromApiAsync(string query, int pageSize, string fromDate ,string cacheKey)
        {
            var emptyResult = new RestNewsClientModel { Articles = new List<Article>() };
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                    try
                    {
                        _logger.LogDebug("Fetching news for query {Query} with page size {PageSize}", query, pageSize);
                        var url = $"everything?q={Uri.EscapeDataString(query)}&from={fromDate}&sortBy=publishedAt&apiKey={_apiKey}&pageSize={pageSize}";
                        
                        using var resp = await _http.GetAsync(url);
                        if (!resp.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("News API returned status code {StatusCode} for query '{Query}'",
                                resp.StatusCode, query);
                            return emptyResult;
                        }
                        var json = await resp.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<RestNewsClientModel>(json) ?? emptyResult;
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

        private static NewsInfoModel ToConsumed(RestNewsClientModel newsResults)
        {
            if (newsResults?.Articles == null)
            {
                return new NewsInfoModel(new List<ConsumedModels.Article>());
            }

            var articles = newsResults.Articles
                .Where(a => !string.IsNullOrEmpty(a?.Title))
                .Select(a => new ConsumedModels.Article(a.Title))
                .ToList();

            return new NewsInfoModel(articles);
        }
        
        private bool IsValidDateFormat(string date)
        {
            return DateTime.TryParseExact(date, _fromDate, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out _);
        }
    }
}