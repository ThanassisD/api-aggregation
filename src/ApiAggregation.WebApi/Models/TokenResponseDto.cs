namespace ApiAggregation.WebApi.Models;

public record TokenResponseDto(string Token, DateTime ExpiresAt);