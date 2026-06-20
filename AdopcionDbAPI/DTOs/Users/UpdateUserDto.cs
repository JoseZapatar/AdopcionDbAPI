using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Users;

public class UpdateUserDto
{
    [Required]
    [StringLength(100)]
    public string name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string email { get; set; } = null!;

    public int roleId { get; set; }
}