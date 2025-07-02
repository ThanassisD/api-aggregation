using System.Net;
using System.Text;
using ApiAggregation.Application.Interfaces;
using ApiAggregation.Domain.Enums;
using ApiAggregation.Infrastructure.Clients;
using ApiAggregation.Infrastructure.Clients.ConsumedModels;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiAggregation.Infrastructure.Tests.Clients;

[TestSubject(typeof(CountryInfoClient))]
public class CountryInfoClientTest
{
    
    private const string ERROR_MESSAGE = "No countries found in the external API.";
    private const string EMPTY_COUNTRY_NAME = "Country name cannot be null or empty.";
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
    
    private ICountryInfoService CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        int cacheDurationMinutes = 60)
    {
        var handler = new FakeHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test/") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<CountryInfoClient>.Instance;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Common:CacheDurationInMinutes", cacheDurationMinutes.ToString())
            })
            .Build();

        return new CountryInfoClient(http, cache, logger, config);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCapitalCityAsync_NullOrWhitespace_ReturnsNullError(string input)
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync(input);

        Assert.Equal(EMPTY_COUNTRY_NAME, result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetCapitalCityAsync_EmptyArray_ReturnsNoCountriesError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Greece");

        Assert.Equal(ERROR_MESSAGE, result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetCapitalCityAsync_HttpRequestException_ReturnsNoCountriesError()
    {
        var client = CreateClient(_ => throw new HttpRequestException("Network down"));

        var result = await client.GetCapitalCityAsync("Greece");

        Assert.Equal(ERROR_MESSAGE, result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetCapitalCityAsync_InvalidJson_ReturnsNoCountriesError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ bad json ", Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Greece");
        
        Assert.Equal(ERROR_MESSAGE, result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetCapitalCityAsync_CountryNotFound_ReturnsNotFoundError()
    {
        var payload = @"[
            { ""name"": { ""common"": ""France"", ""official"": ""French Republic"" }, 
              ""capital"": [""Paris""] }
        ]";

        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Spain");

        Assert.Equal("Country not found in the external API.", result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetCapitalCityAsync_ExactOfficialMatch_ReturnsCapital()
    {
        var payload = @"[
            { ""name"": { ""common"": ""India"", ""official"": ""Republic of India"" }, 
              ""capital"": [""New Delhi""] }
        ]";

        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Republic of India");

        // Assert wrapper
        Assert.Equal("Success", result.Message);
        Assert.Equal(ResponseStatus.Success.GetStatus(), result.Status);
        Assert.NotNull(result.Data);
        
        // Assert data
        var info = Assert.IsType<CountryInfoModel>(result.Data);
        Assert.Equal("New Delhi", info.CapitalCity);
        Assert.Equal("India", info.Name);
    }

    [Fact]
    public async Task GetCapitalCityAsync_PartialCommonMatch_ReturnsCapital()
    {
        var payload = @"[
            { ""name"": { ""common"": ""United Kingdom of Great Britain"", ""official"": ""United Kingdom"" }, 
              ""capital"": [""London""] }
        ]";

        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Kingdom");

        
        // Assert wrapper
        Assert.Equal("Success", result.Message);
        Assert.Equal(ResponseStatus.Success.GetStatus(), result.Status);
        Assert.NotNull(result.Data);
        
        // Assert data
        var info = Assert.IsType<CountryInfoModel>(result.Data);
        Assert.Equal("London", info.CapitalCity);
        Assert.Equal("United Kingdom of Great Britain", info.Name);
    }

    [Fact]
    public async Task GetCapitalCityAsync_NoCapitalInData_ReturnsNoCapitalMessage()
    {
        var payload = @"[
            { ""name"": { ""common"": ""Noland"", ""official"": ""Nolandia"" }, 
              ""capital"": [] }
        ]";

        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

        var result = await client.GetCapitalCityAsync("Narnia");

        Assert.Equal("Country not found in the external API.", result.Message);
        Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
        Assert.Null(result.Data);
    }
}