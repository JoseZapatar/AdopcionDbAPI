using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Pets;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PetsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PetListDto>>> GetPets(
        [FromQuery] int? speciesId,
        [FromQuery] int? breedId,
        [FromQuery] int? sizeId,
        [FromQuery] int? statusId,
        [FromQuery] string? search
    )
    {
        var query = _context.Pets
            .AsNoTracking()
            .Include(p => p.species)
            .Include(p => p.breed)
            .Include(p => p.size)
            .Include(p => p.status)
            .Include(p => p.PetImages)
            .AsQueryable();

        if (speciesId.HasValue)
            query = query.Where(p => p.speciesId == speciesId.Value);

        if (breedId.HasValue)
            query = query.Where(p => p.breedId == breedId.Value);

        if (sizeId.HasValue)
            query = query.Where(p => p.sizeId == sizeId.Value);

        if (statusId.HasValue)
            query = query.Where(p => p.statusId == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.name.Contains(search));

        var pets = await query
            .OrderByDescending(p => p.createdAt)
            .Select(p => new PetListDto
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
                primaryImageId = p.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(pets);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PetListDto>> GetPet(int id)
    {
        var pet = await _context.Pets
            .AsNoTracking()
            .Include(p => p.species)
            .Include(p => p.breed)
            .Include(p => p.size)
            .Include(p => p.status)
            .Include(p => p.PetImages)
            .Where(p => p.id == id)
            .Select(p => new PetListDto
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
                primaryImageId = p.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (pet == null)
            return NotFound("Pet not found.");

        return Ok(pet);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreatePet(CreatePetDto dto)
    {
        var validationResult = await ValidatePetCatalogs(dto.speciesId, dto.sizeId, dto.statusId, dto.breedId);

        if (validationResult != null)
            return validationResult;

        var pet = new Pet
        {
            speciesId = dto.speciesId,
            sizeId = dto.sizeId,
            statusId = dto.statusId,
            breedId = dto.breedId,
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

        return CreatedAtAction(nameof(GetPet), new { id = pet.id }, new
        {
            message = "Pet created successfully.",
            petId = pet.id
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdatePet(int id, UpdatePetDto dto)
    {
        var pet = await _context.Pets.FindAsync(id);

        if (pet == null)
            return NotFound("Pet not found.");

        var validationResult = await ValidatePetCatalogs(dto.speciesId, dto.sizeId, dto.statusId, dto.breedId);

        if (validationResult != null)
            return validationResult;

        pet.speciesId = dto.speciesId;
        pet.sizeId = dto.sizeId;
        pet.statusId = dto.statusId;
        pet.breedId = dto.breedId;
        pet.name = dto.name.Trim();
        pet.gender = dto.gender;
        pet.description = dto.description;
        pet.birthDate = dto.birthDate;
        pet.isVaccinated = dto.isVaccinated;
        pet.isSterilized = dto.isSterilized;
        pet.isDewormed = dto.isDewormed;
        pet.medicalNotes = dto.medicalNotes;
        pet.rescuedAt = dto.rescuedAt;
        pet.updatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Pet updated successfully.",
            petId = pet.id
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeletePet(int id)
    {
        var pet = await _context.Pets.FindAsync(id);

        if (pet == null)
            return NotFound("Pet not found.");

        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Pet deleted successfully.",
            petId = id
        });
    }

    private async Task<ActionResult?> ValidatePetCatalogs(
        int speciesId,
        int sizeId,
        int statusId,
        int? breedId
    )
    {
        var speciesExists = await _context.Species.AnyAsync(s => s.id == speciesId);
        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var sizeExists = await _context.Sizes.AnyAsync(s => s.id == sizeId);
        if (!sizeExists)
            return BadRequest("Invalid sizeId.");

        var statusExists = await _context.PetStatuses.AnyAsync(s => s.id == statusId);
        if (!statusExists)
            return BadRequest("Invalid statusId.");

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