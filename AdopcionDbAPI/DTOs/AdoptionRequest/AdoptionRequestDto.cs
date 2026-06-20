namespace AdopcionDbAPI.DTOs.AdoptionRequests;

public class AdoptionRequestDto
{
    public int id { get; set; }

    public int adopterId { get; set; }

    public string adopterName { get; set; } = null!;

    public string adopterEmail { get; set; } = null!;

    public string? adopterPhone { get; set; }

    public string? adopterCity { get; set; }

    public int petId { get; set; }

    public string petName { get; set; } = null!;

    public string speciesName { get; set; } = null!;

    public string? breedName { get; set; }

    public int statusId { get; set; }

    public string requestStatus { get; set; } = null!;

    public string? message { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }

    public int? reviewedByUserId { get; set; }

    public string? reviewedByUserName { get; set; }

    public DateTime? reviewedAt { get; set; }

    public string? decisionNotes { get; set; }
}