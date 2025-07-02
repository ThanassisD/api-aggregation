using System.Net;
using System.Text;
using ApiAggregation.Domain.Enums;
using ApiAggregation.Infrastructure.Clients;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiAggregation.Infrastructure.Tests.Clients;

[TestSubject(typeof(NewsApiClient))]
public class NewsApiClientTest
{
        private class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
                => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_responder(request));
        }

        private NewsApiClient CreateClient(
            Func<HttpRequestMessage, HttpResponseMessage> responder,
            int cacheMinutes = 60,
            string dateFormat = "yyyy-MM-dd")
        {
            var handler = new FakeHandler(responder);
            var http    = new HttpClient(handler) { BaseAddress = new Uri("https://test/") };
            var cache   = new MemoryCache(new MemoryCacheOptions());
            var logger  = NullLogger<NewsApiClient>.Instance;

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string,string>
                {
                    ["Common:CacheDurationInMinutes"]   = cacheMinutes.ToString(),
                    ["ApiKeys:NewsApiKey"]              = "TestKey",
                    ["ExternalApis:NewsDateFormat"]     = dateFormat
                })
                .Build();

            return new NewsApiClient(http, cache, logger, config);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetTopHeadlinesAsync_EmptyQuery_ReturnsQueryNullError(string query)
        {
            // Arrange
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await client.GetTopHeadlinesAsync(query, "2025-07-02", 10);
            var first = result.Single();

            // Assert
            Assert.Equal("Search query must be provided.", first.Message);
            Assert.Equal(ResponseStatus.Error.GetStatus(), first.Status);
            Assert.Null(first.Data);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_InvalidDateFormat_ReturnsDateFormatError()
        {
            // Arrange
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await client.GetTopHeadlinesAsync("Greece", "07-02-2025", 5);
            var first = result.Single();

            // Assert
            Assert.Equal("Please use this date format: yyyy-MM-dd", first.Message);
            Assert.Equal(ResponseStatus.Error.GetStatus(), first.Status);
            Assert.Null(first.Data);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_HttpError_ReturnsNoNewsFound()
        {
            // Arrange
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act
            var result = await client.GetTopHeadlinesAsync("Greece", "2025-07-02", 5);
            var first = result.Single();

            // Assert
            Assert.Equal("No news articles found.", first.Message);
            Assert.Equal(ResponseStatus.Error.GetStatus(), first.Status);
            Assert.Null(first.Data);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_InvalidJson_ReturnsNoNewsFound()
        {
            // Arrange
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ bad json", Encoding.UTF8, "application/json")
            });

            // Act
            var result = await client.GetTopHeadlinesAsync("Greece", "2025-07-02", 5);
            var first = result.Single();
            
            // Assert
            Assert.Equal("No news articles found.", first.Message);
            Assert.Equal(ResponseStatus.Error.GetStatus(), first.Status);
            Assert.Null(first.Data);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_EmptyArticles_ReturnsNoNewsFound()
        {
            // Arrange
            var payload = @"{ ""articles"": [] }";
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            // Act
            var result = await client.GetTopHeadlinesAsync("Greece", "2025-07-02", 5);
            var first = result.Single();

            // Assert
            Assert.Equal("No news articles found.", first.Message);
            Assert.Equal(ResponseStatus.Error.GetStatus(), first.Status);
            Assert.Null(first.Data);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_ValidArticles_ReturnsSuccessAndData()
        {
            // Arrange
            var payload = @"{
                ""articles"": [
                    { ""title"": ""Headline1"" },
                    { ""title"": ""Headline2"" }
                ]
            }";
            
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            // Act
            var result = await client.GetTopHeadlinesAsync("Greece", "2025-07-02", 2);
            var first = result.Single();

            // Assert
            Assert.Equal("Success", first.Message);
            Assert.Equal(ResponseStatus.Success.GetStatus(), first.Status);
            Assert.NotNull(first.Data);

            // Verify the data payload is a NewsInfoModel with two articles
            var newsInfo = Assert.IsType<NewsInfoModel>(first.Data);
            Assert.Equal(2, newsInfo.Articles.Count);
            Assert.Equal("Headline1", newsInfo.Articles[0].Title);
            Assert.Equal("Headline2", newsInfo.Articles[1].Title);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_MixedEmptyAndValidTitles_FiltersOutEmpty()
        {
            // Arrange
            var payload = @"{
                ""articles"": [
                    { ""title"": """" },
                    { ""title"": null },
                    { ""title"": ""Real Headline"" }
                ]
            }";

            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            // Act
            var result = await client.GetTopHeadlinesAsync("Query", "2025-07-02", 3);
            var first = result.Single();

            // Assert
            Assert.Equal("Success", first.Message);
            Assert.Equal(ResponseStatus.Success.GetStatus(), first.Status);

            var newsInfo = Assert.IsType<NewsInfoModel>(first.Data);
            Assert.Single(newsInfo.Articles);
            Assert.Equal("Real Headline", newsInfo.Articles[0].Title);
        }
    }