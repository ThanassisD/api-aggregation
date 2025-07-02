using System.Net.Http.Headers;
using ApiAggregation.Application.Interfaces;
using ApiAggregation.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace ApiAggregation.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        
        ConfigureApiClient<ICountryInfoService, CountryInfoClient>(services, configuration, "ExternalApis:CountryBaseUrl");
        ConfigureApiClient<IWeatherApiClient, WeatherApiClient>(services, configuration, "ExternalApis:WeatherBaseUrl");
        ConfigureApiClient<INewsApiClient, NewsApiClient>(services, configuration, "ExternalApis:NewsBaseUrl");

        return services;
    }

    private static void ConfigureApiClient<TInterface, TImplementation>(
        IServiceCollection services, 
        IConfiguration configuration,
        string baseUrlConfigKey) 
        where TImplementation : class, TInterface 
        where TInterface : class
    {
        services.AddHttpClient<TInterface, TImplementation>(client =>
            {
                client.BaseAddress = new Uri(configuration[baseUrlConfigKey]!);
                client.DefaultRequestHeaders.Add("User-Agent", "ApiAggregation/1.0");
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });
    }
}