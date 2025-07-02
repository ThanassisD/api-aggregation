namespace ApiAggregation.Domain.Enums;

public static class ResponseStatusExtensions
{
    public static string GetStatus(this ResponseStatus status) => status switch
    {
        ResponseStatus.Success => "Success",
        ResponseStatus.Error => "Error",
        ResponseStatus.NotFound => "NotFound",
        ResponseStatus.BadRequest => "BadRequest",
        ResponseStatus.Unauthorized => "Unauthorized",
        _ => "Unknown"
    };
}