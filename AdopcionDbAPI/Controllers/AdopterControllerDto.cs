using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Adopters;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AdoptersController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdoptersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult<IEnumerable<AdopterDto>>> GetAdopters(
        [FromQuery] string? city,
        [FromQuery] string? search
    )
    {
        var query = _context.Adopters
            .AsNoTracking()
            .Include(a => a.user)
            .Include(a => a.AdoptionRequests)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(a => a.city != null && a.city.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.user.name.Contains(search) ||
                a.user.email.Contains(search)
            );
        }

        var adopters = await query
            .OrderBy(a => a.user.name)
            .Select(a => new AdopterDto
            {
                id = a.id,
                userId = a.userId,
                userName = a.user.name,
                email = a.user.email,
                phone = a.phone,
                address = a.address,
                city = a.city,
                housingType = a.housingType,
                hasOtherPets = a.hasOtherPets,
                createdAt = a.createdAt,
                updatedAt = a.updatedAt,
                totalRequests = a.AdoptionRequests.Count
            })
            .ToListAsync();

        return Ok(adopters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdopterDto>> GetAdopter(int id)
    {
        var adopter = await _context.Adopters
            .AsNoTracking()
            .Include(a => a.user)
            .Include(a => a.AdoptionRequests)
            .Where(a => a.id == id)
            .Select(a => new AdopterDto
            {
                id = a.id,
                userId = a.userId,
                userName = a.user.name,
                email = a.user.email,
                phone = a.phone,
                address = a.address,
                city = a.city,
                housingType = a.housingType,
                hasOtherPets = a.hasOtherPets,
                createdAt = a.createdAt,
                updatedAt = a.updatedAt,
                totalRequests = a.AdoptionRequests.Count
            })
            .FirstOrDefaultAsync();

        if (adopter == null)
            return NotFound("Adopter not found.");

        return Ok(adopter);
    }

    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<AdopterDto>> GetAdopterByUser(int userId)
    {
        var adopter = await _context.Adopters
            .AsNoTracking()
            .Include(a => a.user)
            .Include(a => a.AdoptionRequests)
            .Where(a => a.userId == userId)
            .Select(a => new AdopterDto
            {
                id = a.id,
                userId = a.userId,
                userName = a.user.name,
                email = a.user.email,
                phone = a.phone,
                address = a.address,
                city = a.city,
                housingType = a.housingType,
                hasOtherPets = a.hasOtherPets,
                createdAt = a.createdAt,
                updatedAt = a.updatedAt,
                totalRequests = a.AdoptionRequests.Count
            })
            .FirstOrDefaultAsync();

        if (adopter == null)
            return NotFound("Adopter profile not found for this user.");

        return Ok(adopter);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAdopter(CreateAdopterDto dto)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.id == dto.userId);

        if (!userExists)
            return BadRequest("Invalid userId.");

        var alreadyHasProfile = await _context.Adopters
            .AnyAsync(a => a.userId == dto.userId);

        if (alreadyHasProfile)
            return BadRequest("This user already has an adopter profile.");

        var adopter = new Adopter
        {
            userId = dto.userId,
            phone = dto.phone,
            address = dto.address,
            city = dto.city,
            housingType = dto.housingType,
            hasOtherPets = dto.hasOtherPets,
            createdAt = DateTime.UtcNow
        };

        _context.Adopters.Add(adopter);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAdopter), new { id = adopter.id }, new
        {
            message = "Adopter profile created successfully.",
            adopterId = adopter.id,
            userId = adopter.userId
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateAdopter(int id, UpdateAdopterDto dto)
    {
        var adopter = await _context.Adopters.FindAsync(id);

        if (adopter == null)
            return NotFound("Adopter not found.");

        adopter.phone = dto.phone;
        adopter.address = dto.address;
        adopter.city = dto.city;
        adopter.housingType = dto.housingType;
        adopter.hasOtherPets = dto.hasOtherPets;
        adopter.updatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Adopter profile updated successfully.",
            adopterId = adopter.id
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult> DeleteAdopter(int id)
    {
        var adopter = await _context.Adopters.FindAsync(id);

        if (adopter == null)
            return NotFound("Adopter not found.");

        _context.Adopters.Remove(adopter);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this adopter because it has adoption requests.");
        }

        return Ok(new
        {
            message = "Adopter deleted successfully.",
            adopterId = id
        });
    }
}