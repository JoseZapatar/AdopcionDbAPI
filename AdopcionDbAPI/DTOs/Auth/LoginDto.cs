using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string email { get; set; } = null!;

    [Required]
    public string password { get; set; } = null!;
}