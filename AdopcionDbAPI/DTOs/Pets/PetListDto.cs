namespace AdopcionDbAPI.DTOs.Pets;

public class PetListDto
{
    public int id { get; set; }
    public string name { get; set; } = null!;
    public string? gender { get; set; }
    public string? description { get; set; }
    public DateOnly? birthDate { get; set; }

    public string speciesName { get; set; } = null!;
    public string? breedName { get; set; }
    public string sizeName { get; set; } = null!;
    public string statusName { get; set; } = null!;

    public bool isVaccinated { get; set; }
    public bool isSterilized { get; set; }
    public bool isDewormed { get; set; }
    public string? medicalNotes { get; set; }
    public DateOnly? rescuedAt { get; set; }

    public int? primaryImageId { get; set; }
}