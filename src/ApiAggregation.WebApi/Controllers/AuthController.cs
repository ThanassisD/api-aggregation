using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiAggregation.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiAggregation.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration config) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GenerateToken([FromBody] LoginRequestDto login)
    {
        var validUser = config["JwtSettings:Username"];
        var validPass = config["JwtSettings:Password"];
        var expiresInMinutes = int.TryParse(config["JwtSettings:TokenExpirationInMinutes"], out var minutes) 
            ? minutes : 60;

        if (login.UserName != validUser || login.Password != validPass)
            return Unauthorized();

        var jwt = config.GetSection("JwtSettings");
        var keyBytes = Encoding.UTF8.GetBytes(jwt["SecretKey"] 
                                              ?? throw new InvalidOperationException("JWT secret key is not configured."));
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var token = new JwtSecurityToken(
            jwt["Issuer"],
            jwt["Audience"],
            new[] { new Claim(ClaimTypes.Name, login.UserName) },
            expires: expires,
            signingCredentials: creds
        );

      return Ok(new TokenResponseDto(
          new JwtSecurityTokenHandler().WriteToken(token),
          expires
      ));
    }
}