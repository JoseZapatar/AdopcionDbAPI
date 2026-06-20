using Microsoft.AspNetCore.Http;

namespace AdopcionDbAPI.DTOs.PetImages;

public class UploadPetImageDto
{
    public IFormFile image { get; set; } = null!;

    public bool isPrimary { get; set; }
}