namespace AdopcionDbAPI.DTOs.Adopters;

public class AdopterDto
{
    public int id { get; set; }

    public int userId { get; set; }

    public string userName { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? phone { get; set; }

    public string? address { get; set; }

    public string? city { get; set; }

    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }

    public int totalRequests { get; set; }
}