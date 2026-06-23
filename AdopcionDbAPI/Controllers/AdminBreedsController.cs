using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Administrador")]
public class AdminBreedsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminBreedsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBreeds()
    {
        var breeds = await _context.Breeds
            .AsNoTracking()
            .OrderBy(b => b.species.name)
            .ThenBy(b => b.name)
            .Select(b => new
            {
                id = b.id,
                name = b.name,
                speciesId = b.speciesId,
                speciesName = b.species.name,
                petsCount = b.Pets.Count
            })
            .ToListAsync();

        return Ok(breeds);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBreed(UpsertAdminBreedDto dto)
    {
        var name = dto.name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Breed name is required.");

        var speciesExists = await _context.Species.AnyAsync(s => s.id == dto.speciesId);

        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var exists = await _context.Breeds
            .AnyAsync(b => b.speciesId == dto.speciesId && b.name.ToLower() == name.ToLower());

        if (exists)
            return BadRequest("Breed already exists for this species.");

        var breed = new Breed
        {
            name = name,
            speciesId = dto.speciesId
        };

        _context.Breeds.Add(breed);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Breed created successfully.",
            id = breed.id,
            name = breed.name,
            speciesId = breed.speciesId
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBreed(int id, UpsertAdminBreedDto dto)
    {
        var breed = await _context.Breeds.FindAsync(id);

        if (breed == null)
            return NotFound("Breed not found.");

        var name = dto.name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Breed name is required.");

        var speciesExists = await _context.Species.AnyAsync(s => s.id == dto.speciesId);

        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var exists = await _context.Breeds
            .AnyAsync(b =>
                b.id != id &&
                b.speciesId == dto.speciesId &&
                b.name.ToLower() == name.ToLower());

        if (exists)
            return BadRequest("Breed already exists for this species.");

        breed.name = name;
        breed.speciesId = dto.speciesId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Breed updated successfully.",
            id = breed.id
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBreed(int id)
    {
        var breed = await _context.Breeds
            .Include(b => b.Pets)
            .FirstOrDefaultAsync(b => b.id == id);

        if (breed == null)
            return NotFound("Breed not found.");

        if (breed.Pets.Any())
            return BadRequest("Cannot delete this breed because it is being used by pets.");

        _context.Breeds.Remove(breed);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Breed deleted successfully.",
            id
        });
    }
}

public class UpsertAdminBreedDto
{
    public int speciesId { get; set; }

    public string name { get; set; } = string.Empty;
}
