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

    [StringLength(20)]
    public string accountType { get; set; } = "adoptar";

    [StringLength(30)]
    public string? phone { get; set; }

    [StringLength(255)]
    public string? address { get; set; }

    [StringLength(100)]
    public string? city { get; set; }

    [StringLength(50)]
    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }
}
