using ApiAggregation.WebApi.Controllers;
using ApiAggregation.WebApi.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiAggregation.WebApi.Tests.Controllers;

[TestSubject(typeof(AuthController))]
public class AuthControllerControllerTest
{
    private AuthController CreateController(Dictionary<string, string> inMemorySettings)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return new AuthController(config);
    }

    [Fact]
    public void GenerateToken_EmptyUserNameAndPassword_ReturnsUnauthorized()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["JwtSettings:Username"] = "user",
            ["JwtSettings:Password"] = "pass",
            ["JwtSettings:SecretKey"] = "some–base64–key",
            ["JwtSettings:Issuer"] = "ApiAggregation",
            ["JwtSettings:Audience"] = "ApiAggregationClients",
            ["JwtSettings:TokenExpirationInMinutes"] = "60"
        };

        var controller = CreateController(settings);
        var request = new LoginRequestDto
        {
            UserName = String.Empty,
            Password = String.Empty
        };

        // Act
        var result = controller.GenerateToken(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GenerateToken_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["JwtSettings:Username"] = "user",
            ["JwtSettings:Password"] = "pass",
            ["JwtSettings:SecretKey"] = "some–base64–key",
            ["JwtSettings:Issuer"] = "ApiAggregation",
            ["JwtSettings:Audience"] = "ApiAggregationClients",
            ["JwtSettings:TokenExpirationInMinutes"] = "60"
        };

        var controller = CreateController(settings);
        var request = new LoginRequestDto
        {
            UserName = "not-user",
            Password = "not-pass"
        };

        // Act
        var result = controller.GenerateToken(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GenerateToken_MissingSecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["JwtSettings:Username"] = "user",
            ["JwtSettings:Password"] = "pass",
            ["JwtSettings:Issuer"] = "ApiAggregation",
            ["JwtSettings:Audience"] = "ApiAggregationClients",
            ["JwtSettings:TokenExpirationInMinutes"] = "60"
        };

        var controller = CreateController(settings);
        var request = new LoginRequestDto
        {
            UserName = "user",
            Password = "pass"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => controller.GenerateToken(request));
        Assert.Equal("JWT secret key is not configured.", ex.Message);
    }
    
    [Fact]
    public void GenerateToken_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["JwtSettings:Username"] = "user",
            ["JwtSettings:Password"] = "pass",
            ["JwtSettings:SecretKey"] = "rQyFs9t9V4PUuqxsaJ+fT8e7dRzSgMw9BQCl2nDF3mA=",
            ["JwtSettings:Issuer"]    = "ApiAggregation",
            ["JwtSettings:Audience"]  = "ApiAggregationClients",
            ["JwtSettings:TokenExpirationInMinutes"] = "60"
        };

        var controller = CreateController(settings);
        var request = new LoginRequestDto
        {
            UserName = "user",
            Password = "pass"
        };

        // Act
        var result = controller.GenerateToken(request);

        // Assert 200 OK
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Assert correct DTO
        var tokenResponse = Assert.IsType<TokenResponseDto>(okResult.Value);
        Assert.False(string.IsNullOrEmpty(tokenResponse.Token), "Token should not be null or empty");
        Assert.True(tokenResponse.ExpiresAt > DateTime.UtcNow, "ExpiresAt should be in the future");
    }
}