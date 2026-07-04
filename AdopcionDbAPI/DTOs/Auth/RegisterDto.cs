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

    [StringLength(150)]
    public string? legalName { get; set; }

    [StringLength(1000)]
    public string? experience { get; set; }

    [StringLength(255)]
    public string? animalTypes { get; set; }

    [StringLength(50)]
    public string? monthlyCapacity { get; set; }

    [StringLength(500)]
    public string? facilityType { get; set; }

    [StringLength(255)]
    public string? availability { get; set; }

    [StringLength(500)]
    public string? motivation { get; set; }

    [StringLength(255)]
    public string? referenceContact { get; set; }

    public bool hasTransport { get; set; }

    public bool acceptsResponsibility { get; set; }

    public IFormFile? identificationImage { get; set; }
}
