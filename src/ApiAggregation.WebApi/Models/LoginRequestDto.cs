using System.ComponentModel.DataAnnotations;

namespace ApiAggregation.WebApi.Models;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = null!;
}