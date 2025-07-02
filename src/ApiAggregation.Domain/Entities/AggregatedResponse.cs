namespace ApiAggregation.Domain.Entities;

public class AggregatedResponse
{
    public List<ApiResponseWrapper>  Aggregate { get; set; }
    
    public string Status { get; set; }
    
    public string Message { get; set; }
    
    public AggregatedResponse(List<ApiResponseWrapper> aggregate, string status, string message)
    {
        Aggregate = aggregate;
        Status = status;
        Message = message;
    }
}