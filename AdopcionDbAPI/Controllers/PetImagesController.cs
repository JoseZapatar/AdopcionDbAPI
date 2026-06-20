using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.PetImages;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PetImagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PetImagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("pet/{petId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PetImageDto>>> GetImagesByPet(int petId)
    {
        var petExists = await _context.Pets.AnyAsync(p => p.id == petId);

        if (!petExists)
            return NotFound("Pet not found.");

        var images = await _context.PetImages
            .AsNoTracking()
            .Where(i => i.petId == petId)
            .OrderByDescending(i => i.isPrimary)
            .ThenByDescending(i => i.createdAt)
            .Select(i => new PetImageDto
            {
                id = i.id,
                petId = i.petId,
                imageContentType = i.imageContentType,
                isPrimary = i.isPrimary,
                createdAt = i.createdAt,
                imageUrl = $"/api/PetImages/{i.id}/file"
            })
            .ToListAsync();

        return Ok(images);
    }

    [HttpGet("{id:int}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImageFile(int id)
    {
        var image = await _context.PetImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.id == id);

        if (image == null)
            return NotFound("Image not found.");

        return File(image.imageData, image.imageContentType);
    }

    [HttpPost("pet/{petId:int}")]
    [Consumes("multipart/form-data")]
    [Authorize (Roles = "Admin")]
    public async Task<ActionResult> UploadImage(int petId, [FromForm] UploadPetImageDto dto)
    {
        var petExists = await _context.Pets.AnyAsync(p => p.id == petId);

        if (!petExists)
            return NotFound("Pet not found.");

        if (dto.image == null || dto.image.Length == 0)
            return BadRequest("Image is required.");

        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedContentTypes.Contains(dto.image.ContentType))
            return BadRequest("Only JPEG, PNG and WEBP images are allowed.");

        const long maxFileSize = 5 * 1024 * 1024;

        if (dto.image.Length > maxFileSize)
            return BadRequest("Image size cannot exceed 5 MB.");

        var hasImages = await _context.PetImages
            .AnyAsync(i => i.petId == petId);

        var shouldBePrimary = dto.isPrimary || !hasImages;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (shouldBePrimary)
        {
            var currentPrimaryImages = await _context.PetImages
                .Where(i => i.petId == petId && i.isPrimary)
                .ToListAsync();

            foreach (var img in currentPrimaryImages)
            {
                img.isPrimary = false;
            }

            await _context.SaveChangesAsync();
        }

        using var memoryStream = new MemoryStream();
        await dto.image.CopyToAsync(memoryStream);

        var petImage = new PetImage
        {
            petId = petId,
            imageData = memoryStream.ToArray(),
            imageContentType = dto.image.ContentType,
            isPrimary = shouldBePrimary,
            createdAt = DateTime.UtcNow
        };

        _context.PetImages.Add(petImage);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetImageFile), new { id = petImage.id }, new
        {
            message = "Image uploaded successfully.",
            imageId = petImage.id,
            petId = petImage.petId,
            isPrimary = petImage.isPrimary,
            imageUrl = $"/api/PetImages/{petImage.id}/file"
        });
    }

    [HttpPut("{id:int}/set-primary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SetPrimaryImage(int id)
    {
        var image = await _context.PetImages
            .FirstOrDefaultAsync(i => i.id == id);

        if (image == null)
            return NotFound("Image not found.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var currentPrimaryImages = await _context.PetImages
            .Where(i => i.petId == image.petId && i.isPrimary)
            .ToListAsync();

        foreach (var img in currentPrimaryImages)
        {
            img.isPrimary = false;
        }

        await _context.SaveChangesAsync();

        image.isPrimary = true;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Primary image updated successfully.",
            imageId = image.id,
            petId = image.petId
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteImage(int id)
    {
        var image = await _context.PetImages
            .FirstOrDefaultAsync(i => i.id == id);

        if (image == null)
            return NotFound("Image not found.");

        var petId = image.petId;
        var wasPrimary = image.isPrimary;

        _context.PetImages.Remove(image);
        await _context.SaveChangesAsync();

        if (wasPrimary)
        {
            var nextImage = await _context.PetImages
                .Where(i => i.petId == petId)
                .OrderByDescending(i => i.createdAt)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                nextImage.isPrimary = true;
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            message = "Image deleted successfully.",
            imageId = id,
            petId
        });
    }
}