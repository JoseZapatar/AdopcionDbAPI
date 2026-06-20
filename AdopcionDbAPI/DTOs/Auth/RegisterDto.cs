using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [StringLength(100)]
    public string name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string password { get; set; } = null!;
}