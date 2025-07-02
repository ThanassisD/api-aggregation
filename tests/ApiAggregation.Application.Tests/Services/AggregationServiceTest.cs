using ApiAggregation.Application.Interfaces;
using ApiAggregation.Application.Services;
using ApiAggregation.Domain.Entities;
using ApiAggregation.Domain.Enums;
using JetBrains.Annotations;
using Moq;

namespace ApiAggregation.Application.Tests.Services;

[TestSubject(typeof(AggregationService))]
public class AggregationServiceTest
{
        private readonly Mock<ICountryInfoService> _countryMock;
        private readonly Mock<IWeatherApiClient>   _weatherMock;
        private readonly Mock<INewsApiClient>      _newsMock;
        private readonly AggregationService        _service;

        public AggregationServiceTest()
        {
            _countryMock = new Mock<ICountryInfoService>();
            _weatherMock = new Mock<IWeatherApiClient>();
            _newsMock    = new Mock<INewsApiClient>();

            _service = new AggregationService(
                _countryMock.Object,
                _weatherMock.Object,
                _newsMock.Object
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAggregatedDataAsync_EmptyCountry_ReturnsBadRequest(string badCountry)
        {
            // Act
            var result = await _service.GetAggregatedDataAsync(badCountry, newsPageSize: 5, fromDate: "2025-07-01");

            // Assert
            Assert.Single(result.Aggregate);
            var wrapper = result.Aggregate[0];
            Assert.Equal("Country name cannot be empty", wrapper.Message);
            Assert.Equal("BadRequest", wrapper.Status);
            Assert.Equal("BadRequest", result.Status);
            Assert.Equal(
                "An error occurred while processing your request. See internal messages for details.",
                result.Message);
        }

        [Fact]
        public async Task GetAggregatedDataAsync_CountryNotFound_ReturnsNotFound()
        {
            // Arrange: country service returns Data=null
            _countryMock
                .Setup(x => x.GetCapitalCityAsync("Narnia"))
                .ReturnsAsync(new ApiResponseWrapper("ignored", "OK", data: null));

            // Act
            var result = await _service.GetAggregatedDataAsync("Narnia", newsPageSize: 5, fromDate: "2025-07-01");

            // Assert
            Assert.Single(result.Aggregate);
            var wrapper = result.Aggregate[0];
            Assert.Equal("Country not found", wrapper.Message);
            Assert.Equal("NotFound", wrapper.Status);
            Assert.Equal("NotFound", result.Status);
            Assert.Equal(
                "An error occurred while processing your request. See internal messages for details.",
                result.Message);
        }

        [Fact]
        public async Task GetAggregatedDataAsync_AllServicesSucceed_ReturnsSuccess()
        {
            // Arrange
            _countryMock
                .Setup(x => x.GetCapitalCityAsync("Greece"))
                .ReturnsAsync(new ApiResponseWrapper("OK", "OK", data: "Athens"));
            
            var weatherJson = "{\"temp\":22.3,\"description\":\"Sunny\"}";
            _weatherMock
                .Setup(x => x.GetWeatherAsync("Athens"))
                .ReturnsAsync(new ApiResponseWrapper("OK", "OK", data: weatherJson));
            
            var newsJson = "[{\"title\":\"Headline1\"},{\"title\":\"Headline2\"}]";
            _newsMock
                .Setup(x => x.GetTopHeadlinesAsync("Greece", "2025-07-01", 5))
                .ReturnsAsync(new[]
                {
                    new ApiResponseWrapper("OK", "OK", data: newsJson)
                });

            // Act
            var result = await _service.GetAggregatedDataAsync("Greece", newsPageSize: 5, fromDate: "2025-07-01");

            // Assert 
            Assert.Equal(3, result.Aggregate.Count);
            Assert.All(result.Aggregate, w => Assert.Equal("OK", w.Status));
            Assert.Equal(ResponseStatus.Success.GetStatus() , result.Status);
            Assert.Equal("All data aggregated successfully.", result.Message);
        }

        [Fact]
        public async Task GetAggregatedDataAsync_WeatherFails_StillReturnsAllWrappersAndErrorStatus()
        {
            // Arrange
            _countryMock
                .Setup(x => x.GetCapitalCityAsync("France"))
                .ReturnsAsync(new ApiResponseWrapper("OK", "OK", data: "Paris"));

            _weatherMock
                .Setup(x => x.GetWeatherAsync("Paris"))
                .ReturnsAsync(new ApiResponseWrapper("Weather service down", "Error"));
            
            _newsMock
                .Setup(x => x.GetTopHeadlinesAsync("France", "2025-07-01", 10))
                .ReturnsAsync(new[]
                {
                    new ApiResponseWrapper("OK", "OK", data: "[{\"title\":\"Headline\"}]")
                });

            // Act
            var result = await _service.GetAggregatedDataAsync("France", newsPageSize: 10, fromDate: "2025-07-01");

            // Assert
            Assert.Equal(3, result.Aggregate.Count);
            Assert.Equal("OK",    result.Aggregate[0].Status);
            Assert.Equal("Error", result.Aggregate[1].Status);
            Assert.Equal("OK",    result.Aggregate[2].Status);
            Assert.Equal("Error", result.Status);
            Assert.Equal("Some errors occurred. Check internal messages for details.", result.Message);
        }

        [Fact]
        public async Task GetAggregatedDataAsync_NewsFails_StillReturnsAllWrappersAndErrorStatus()
        {
            // Arrange
            _countryMock
                .Setup(x => x.GetCapitalCityAsync("Germany"))
                .ReturnsAsync(new ApiResponseWrapper("OK", "OK", data: "Berlin"));

            _weatherMock
                .Setup(x => x.GetWeatherAsync("Berlin"))
                .ReturnsAsync(new ApiResponseWrapper("OK", "OK", data: "{\"temp\":10}"));
            
            _newsMock
                .Setup(x => x.GetTopHeadlinesAsync("Germany", "2025-07-01", 10))
                .ReturnsAsync(new[]
                {
                    new ApiResponseWrapper("News service timeout", "Error")
                });

            // Act
            var result = await _service.GetAggregatedDataAsync("Germany", newsPageSize: 10, fromDate: "2025-07-01");

            // Assert
            Assert.Equal(3, result.Aggregate.Count);
            Assert.Equal("OK",    result.Aggregate[0].Status);
            Assert.Equal("OK",    result.Aggregate[1].Status);
            Assert.Equal("Error", result.Aggregate[2].Status);
            Assert.Equal(ResponseStatus.Error.GetStatus(), result.Status);
            Assert.Equal("Some errors occurred. Check internal messages for details.", result.Message);
        }
    }