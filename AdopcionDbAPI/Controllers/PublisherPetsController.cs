using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Publicador")]
public class PublisherPetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublisherPetsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("my-pets")]
    public async Task<IActionResult> GetMyPets()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var pets = await _context.Pets
            .AsNoTracking()
            .Where(p => p.publisherUserId == userId.Value)
            .OrderByDescending(p => p.createdAt)
            .Select(p => new
            {
                id = p.id,
                name = p.name,
                gender = p.gender,
                description = p.description,
                birthDate = p.birthDate,
                speciesName = p.species.name,
                breedName = p.breed != null ? p.breed.name : null,
                sizeName = p.size.name,
                statusName = p.status.name,
                isVaccinated = p.isVaccinated,
                isSterilized = p.isSterilized,
                isDewormed = p.isDewormed,
                medicalNotes = p.medicalNotes,
                rescuedAt = p.rescuedAt,
                createdAt = p.createdAt,
                updatedAt = p.updatedAt,
                primaryImageId = p.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault(),
                requestsCount = p.AdoptionRequests.Count,
                pendingRequestsCount = p.AdoptionRequests.Count(r => r.status.name.ToLower() == "pendiente")
            })
            .ToListAsync();

        return Ok(pets);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var requests = await _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.pet.publisherUserId == userId.Value)
            .Where(r => r.status.name.ToLower() == "pendiente")
            .OrderByDescending(r => r.createdAt)
            .Select(r => new
            {
                id = r.id,
                petId = r.petId,
                petName = r.pet.name,
                adopterId = r.adopterId,
                adopterName = r.adopter.user.name,
                adopterEmail = r.adopter.user.email,
                adopterPhone = r.adopter.phone,
                adopterCity = r.adopter.city,
                message = r.message,
                statusId = r.statusId,
                requestStatus = r.status.name,
                createdAt = r.createdAt,
                updatedAt = r.updatedAt,
                decisionNotes = r.decisionNotes
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var history = await _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.pet.publisherUserId == userId.Value)
            .Where(r => r.status.name.ToLower() == "aprobada")
            .OrderByDescending(r => r.reviewedAt ?? r.updatedAt ?? r.createdAt)
            .Select(r => new
            {
                id = r.id,
                petId = r.petId,
                petName = r.pet.name,
                speciesName = r.pet.species.name,
                breedName = r.pet.breed != null ? r.pet.breed.name : null,
                adopterName = r.adopter.user.name,
                adopterEmail = r.adopter.user.email,
                adopterPhone = r.adopter.phone,
                adopterCity = r.adopter.city,
                message = r.message,
                adoptedAt = r.reviewedAt ?? r.updatedAt ?? r.createdAt,
                decisionNotes = r.decisionNotes,
                primaryImageId = r.pet.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePet(CreatePublisherPetDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var availableStatusId = await _context.PetStatuses
            .Where(s => s.name.ToLower() == "disponible")
            .Select(s => (int?)s.id)
            .FirstOrDefaultAsync();

        if (availableStatusId == null)
            return BadRequest("Pet status 'Disponible' was not found.");

        var validationResult = await ValidatePetCatalogs(dto.speciesId, dto.sizeId, dto.breedId);

        if (validationResult != null)
            return validationResult;

        var pet = new Pet
        {
            speciesId = dto.speciesId,
            breedId = dto.breedId,
            sizeId = dto.sizeId,
            statusId = availableStatusId.Value,
            publisherUserId = userId.Value,
            name = dto.name.Trim(),
            gender = dto.gender,
            description = dto.description,
            birthDate = dto.birthDate,
            isVaccinated = dto.isVaccinated,
            isSterilized = dto.isSterilized,
            isDewormed = dto.isDewormed,
            medicalNotes = dto.medicalNotes,
            rescuedAt = dto.rescuedAt,
            createdAt = DateTime.UtcNow
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyPets), new { id = pet.id }, new
        {
            message = "Pet published successfully.",
            petId = pet.id
        });
    }

    [HttpPost("{petId:int}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(int petId, [FromForm] UploadPublisherPetImageDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var pet = await _context.Pets
            .FirstOrDefaultAsync(p => p.id == petId && p.publisherUserId == userId.Value);

        if (pet == null)
            return NotFound("Pet not found.");

        if (dto.image == null || dto.image.Length == 0)
            return BadRequest("Image is required.");

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        if (!allowedContentTypes.Contains(dto.image.ContentType))
            return BadRequest("Only JPEG, PNG and WEBP images are allowed.");

        const long maxFileSize = 5 * 1024 * 1024;

        if (dto.image.Length > maxFileSize)
            return BadRequest("Image size cannot exceed 5 MB.");

        var hasImages = await _context.PetImages.AnyAsync(i => i.petId == petId);
        var shouldBePrimary = dto.isPrimary || !hasImages;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (shouldBePrimary)
        {
            var currentPrimaryImages = await _context.PetImages
                .Where(i => i.petId == petId && i.isPrimary)
                .ToListAsync();

            foreach (var image in currentPrimaryImages)
            {
                image.isPrimary = false;
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

        return Ok(new
        {
            message = "Image uploaded successfully.",
            imageId = petImage.id,
            petId = petImage.petId,
            imageUrl = $"/api/PetImages/{petImage.id}/file"
        });
    }

    [HttpPut("requests/{requestId:int}/approve")]
    public async Task<IActionResult> ApproveRequest(int requestId, PublisherReviewRequestDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var pendingStatusId = await GetRequestStatusId("Pendiente");
        var approvedStatusId = await GetRequestStatusId("Aprobada");
        var cancelledStatusId = await GetRequestStatusId("Cancelada");

        if (pendingStatusId == null || approvedStatusId == null || cancelledStatusId == null)
            return BadRequest("Required request statuses were not found.");

        var adoptedPetStatusId = await _context.PetStatuses
            .Where(s => s.name.ToLower() == "adoptado")
            .Select(s => (int?)s.id)
            .FirstOrDefaultAsync();

        if (adoptedPetStatusId == null)
            return BadRequest("Pet status 'Adoptado' was not found.");

        var request = await _context.AdoptionRequests
            .Include(r => r.pet)
            .FirstOrDefaultAsync(r => r.id == requestId && r.pet.publisherUserId == userId.Value);

        if (request == null)
            return NotFound("Adoption request not found.");

        if (request.statusId != pendingStatusId.Value)
            return BadRequest("Only pending requests can be approved.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        request.statusId = approvedStatusId.Value;
        request.reviewedByUserId = userId.Value;
        request.reviewedAt = DateTime.UtcNow;
        request.updatedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud aprobada por el publicador."
            : dto.decisionNotes.Trim();

        request.pet.statusId = adoptedPetStatusId.Value;
        request.pet.updatedAt = DateTime.UtcNow;

        var otherPendingRequests = await _context.AdoptionRequests
            .Where(r =>
                r.petId == request.petId &&
                r.id != request.id &&
                r.statusId == pendingStatusId.Value)
            .ToListAsync();

        foreach (var otherRequest in otherPendingRequests)
        {
            otherRequest.statusId = cancelledStatusId.Value;
            otherRequest.reviewedByUserId = userId.Value;
            otherRequest.reviewedAt = DateTime.UtcNow;
            otherRequest.updatedAt = DateTime.UtcNow;
            otherRequest.decisionNotes = "Solicitud cancelada porque la mascota ya fue adoptada.";
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Adoption request approved successfully.",
            requestId = request.id,
            petId = request.petId,
            cancelledRequests = otherPendingRequests.Count
        });
    }

    [HttpPut("requests/{requestId:int}/reject")]
    public async Task<IActionResult> RejectRequest(int requestId, PublisherReviewRequestDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var pendingStatusId = await GetRequestStatusId("Pendiente");
        var rejectedStatusId = await GetRequestStatusId("Rechazada");

        if (pendingStatusId == null || rejectedStatusId == null)
            return BadRequest("Required request statuses were not found.");

        var request = await _context.AdoptionRequests
            .Include(r => r.pet)
            .FirstOrDefaultAsync(r => r.id == requestId && r.pet.publisherUserId == userId.Value);

        if (request == null)
            return NotFound("Adoption request not found.");

        if (request.statusId != pendingStatusId.Value)
            return BadRequest("Only pending requests can be rejected.");

        request.statusId = rejectedStatusId.Value;
        request.reviewedByUserId = userId.Value;
        request.reviewedAt = DateTime.UtcNow;
        request.updatedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud rechazada por el publicador."
            : dto.decisionNotes.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Adoption request rejected successfully.",
            requestId = request.id
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return userId;
    }

    private async Task<int?> GetRequestStatusId(string statusName)
    {
        var normalized = statusName.ToLower();

        return await _context.RequestStatuses
            .Where(s => s.name.ToLower() == normalized)
            .Select(s => (int?)s.id)
            .FirstOrDefaultAsync();
    }

    private async Task<IActionResult?> ValidatePetCatalogs(int speciesId, int sizeId, int? breedId)
    {
        var speciesExists = await _context.Species.AnyAsync(s => s.id == speciesId);

        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var sizeExists = await _context.Sizes.AnyAsync(s => s.id == sizeId);

        if (!sizeExists)
            return BadRequest("Invalid sizeId.");

        if (breedId.HasValue)
        {
            var breedBelongsToSpecies = await _context.Breeds
                .AnyAsync(b => b.id == breedId.Value && b.speciesId == speciesId);

            if (!breedBelongsToSpecies)
                return BadRequest("The selected breed does not belong to the selected species.");
        }

        return null;
    }
}

public class CreatePublisherPetDto
{
    public int speciesId { get; set; }

    public int sizeId { get; set; }

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

public class UploadPublisherPetImageDto
{
    public IFormFile? image { get; set; }

    public bool isPrimary { get; set; } = true;
}

public class PublisherReviewRequestDto
{
    public string? decisionNotes { get; set; }
}
