namespace AdopcionDbAPI.DTOs.Pets;

public class UpdatePetDto
{
    public int speciesId { get; set; }
    public int sizeId { get; set; }
    public int statusId { get; set; }
    public int? breedId { get; set; }

    public string name { get; set; } = null!;
    public string? gender { get; set; }
    public string? description { get; set; }
    public DateOnly? birthDate { get; set; }

    public bool isVaccinated { get; set; }
    public bool isSterilized { get; set; }
    public bool isDewormed { get; set; }
    public string? medicalNotes { get; set; }
    public DateOnly? rescuedAt { get; set; }
}