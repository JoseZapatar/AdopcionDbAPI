namespace AdopcionDbAPI.DTOs.PetImages;

public class PetImageDto
{
    public int id { get; set; }

    public int petId { get; set; }

    public string imageContentType { get; set; } = null!;

    public bool isPrimary { get; set; }

    public DateTime createdAt { get; set; }

    public string imageUrl { get; set; } = null!;
}