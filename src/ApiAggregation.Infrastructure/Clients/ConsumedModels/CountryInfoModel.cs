namespace ApiAggregation.Infrastructure.Clients.ConsumedModels;

public class CountryInfoModel
{
    public string Name { get;} 
    public string CapitalCity { get; }
    
    public CountryInfoModel(string name, string capitalCity)
    {
        Name = name;
        CapitalCity = capitalCity;
    }
    
    public string GetCapitalCity()
    {
        return CapitalCity;
    }
}