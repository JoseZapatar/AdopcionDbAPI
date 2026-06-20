using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Users;

public class ChangePasswordDto
{
    [Required]
    [MinLength(6)]
    public string newPassword { get; set; } = null!;
}