using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.AdoptionRequests;

public class ReviewAdoptionRequestDto
{
    public int reviewedByUserId { get; set; }

    [StringLength(1000)]
    public string? decisionNotes { get; set; }
}