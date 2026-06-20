using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Adopters;

public class UpdateAdopterDto
{
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