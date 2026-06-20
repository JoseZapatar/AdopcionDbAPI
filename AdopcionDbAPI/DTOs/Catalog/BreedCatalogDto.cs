namespace AdopcionDbAPI.DTOs.Catalogs;

public class BreedCatalogDto
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public int speciesId { get; set; }

    public string speciesName { get; set; } = null!;
}