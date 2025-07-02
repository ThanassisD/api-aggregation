namespace ApiAggregation.Domain.Entities;

public class ApiResponseWrapper
{
    public string Message { get;  }

    public string Status { get; }

    public Object? Data { get; }
    
    public ApiResponseWrapper(string message, string status, Object? data = null)
    {
        Message = message;
        Status = status;
        Data = data;
    }
}