using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.AdoptionRequests;

public class CreateAdoptionRequestDto
{
    public int? adopterId { get; set; }

    public int petId { get; set; }

    [StringLength(100)]
    public string? adopterName { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? adopterEmail { get; set; }

    [StringLength(30)]
    public string? adopterPhone { get; set; }

    [StringLength(100)]
    public string? adopterCity { get; set; }

    [StringLength(255)]
    public string? address { get; set; }

    [StringLength(50)]
    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }

    [StringLength(1000)]
    public string? message { get; set; }
}
