namespace ApiAggregation.Infrastructure.Clients.ConsumedModels;

public class WeatherInfoModel
{
    public double Temperature { get; }
    public string Weather { get; }

    public WeatherInfoModel(double temperature, string weather)
    {
        Temperature = temperature;
        Weather = weather;
    }
}