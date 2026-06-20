using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.AdoptionRequests;

public class CreateAdoptionRequestDto
{
    public int adopterId { get; set; }

    public int petId { get; set; }

    [StringLength(1000)]
    public string? message { get; set; }
}