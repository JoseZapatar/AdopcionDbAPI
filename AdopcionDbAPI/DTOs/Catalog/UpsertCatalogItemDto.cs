using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Catalogs;

public class UpsertCatalogItemDto
{
    [Required]
    [StringLength(100)]
    public string name { get; set; } = null!;
}