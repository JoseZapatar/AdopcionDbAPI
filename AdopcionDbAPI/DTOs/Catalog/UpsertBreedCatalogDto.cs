using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Catalogs;

public class UpsertBreedDto
{
    public int speciesId { get; set; }

    [Required]
    [StringLength(100)]
    public string name { get; set; } = null!;
}