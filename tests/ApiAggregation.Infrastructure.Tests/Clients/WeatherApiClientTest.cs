using System.Net;
using System.Text;
using ApiAggregation.Application.Interfaces;
using ApiAggregation.Infrastructure.Clients;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiAggregation.Infrastructure.Tests.Clients;

[TestSubject(typeof(WeatherApiClient))]
public class WeatherApiClientTest
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) 
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }

    private IWeatherApiClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        int cacheMinutes = 60)
    {
        var handler = new FakeHandler(responder);
        var http    = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var cache   = new MemoryCache(new MemoryCacheOptions());
        var logger  = NullLogger<WeatherApiClient>.Instance;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string,string>("Common:CacheDurationInMinutes", cacheMinutes.ToString()),
                new KeyValuePair<string,string>("ApiKeys:OpenWeatherKey", "TestKey")
            })
            .Build();

        return new WeatherApiClient(http, cache, logger, config);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetWeatherAsync_NullOrWhitespace_ReturnsError(string input)
    {
        // Arrange
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await client.GetWeatherAsync(input);

        // Assert
        Assert.Equal("City name must be provided.", result.Message);
        Assert.Equal("Error", result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetWeatherAsync_HttpStatusNotSuccess_ReturnsNotFoundError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        // Act
        var result = await client.GetWeatherAsync("Athens");

        // Assert
        Assert.Equal("Weather Not found.", result.Message);
        Assert.Equal("Error", result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetWeatherAsync_EmptyWeatherArray_ReturnsNotFoundError()
    {
        // Arrange
        var payload = @"{
            ""weather"":[],
            ""main"":{""temp"":15.5}
        }";
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await client.GetWeatherAsync("Athens");

        // Assert
        Assert.Equal("Weather Not found.", result.Message);
        Assert.Equal("Error", result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetWeatherAsync_InvalidJson_ReturnsNotFoundError()
    {
        // Arrange: malformed JSON
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json", Encoding.UTF8, "application/json")
        });

        // Act
        var result = await client.GetWeatherAsync("Athens");

        // Assert
        Assert.Equal("Weather Not found.", result.Message);
        Assert.Equal("Error", result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetWeatherAsync_ValidJson_ReturnsSuccessAndData()
    {
        // Arrange
        var payload = @"{
            ""weather"":[{""description"":""Sunny""}],
            ""main"":{""temp"":22.3}
        }";
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await client.GetWeatherAsync("Athens");

        // Assert wrapper
        Assert.Equal("Success", result.Message);
        Assert.Equal("OK", result.Status);
        Assert.NotNull(result.Data);

        // Assert data
        var info = Assert.IsType<WeatherInfoModel>(result.Data);
        Assert.Equal(22.3, info.Temperature, 1);
        Assert.Equal("Sunny", info.Weather);
    }
}