using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Administrador")]
public class AdminSpeciesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminSpeciesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSpecies()
    {
        var species = await _context.Species
            .AsNoTracking()
            .OrderBy(s => s.name)
            .Select(s => new
            {
                id = s.id,
                name = s.name,
                breedsCount = _context.Breeds.Count(b => b.speciesId == s.id),
                petsCount = _context.Pets.Count(p => p.speciesId == s.id)
            })
            .ToListAsync();

        return Ok(species);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSpecies(SpeciesRequestDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.Species
            .AnyAsync(s => s.name.ToLower() == name.ToLower());

        if (exists)
            return BadRequest("Ya existe una especie con ese nombre.");

        var species = new Species
        {
            name = name
        };

        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Especie creada correctamente.",
            id = species.id,
            name = species.name
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSpecies(int id, SpeciesRequestDto dto)
    {
        var species = await _context.Species.FirstOrDefaultAsync(s => s.id == id);

        if (species == null)
            return NotFound("Especie no encontrada.");

        var name = dto.name.Trim();

        var exists = await _context.Species
            .AnyAsync(s => s.id != id && s.name.ToLower() == name.ToLower());

        if (exists)
            return BadRequest("Ya existe otra especie con ese nombre.");

        species.name = name;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Especie actualizada correctamente.",
            id = species.id,
            name = species.name
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSpecies(int id)
    {
        var species = await _context.Species.FirstOrDefaultAsync(s => s.id == id);

        if (species == null)
            return NotFound("Especie no encontrada.");

        var hasBreeds = await _context.Breeds.AnyAsync(b => b.speciesId == id);
        var hasPets = await _context.Pets.AnyAsync(p => p.speciesId == id);

        if (hasBreeds || hasPets)
            return BadRequest("No se puede eliminar la especie porque tiene razas o mascotas asociadas.");

        _context.Species.Remove(species);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Especie eliminada correctamente."
        });
    }
}

public class SpeciesRequestDto
{
    [Required]
    [StringLength(100)]
    public string name { get; set; } = string.Empty;
}
