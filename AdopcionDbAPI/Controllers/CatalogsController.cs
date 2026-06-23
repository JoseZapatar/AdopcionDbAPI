using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Catalogs;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CatalogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogsController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // SPECIES
    // =========================

    [HttpGet("species")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetSpecies()
    {
        var data = await _context.Species
            .AsNoTracking()
            .OrderBy(x => x.name)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("species/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CatalogItemDto>> GetSpeciesById(int id)
    {
        var item = await _context.Species
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Species not found.");

        return Ok(item);
    }

    [HttpPost("species")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<ActionResult> CreateSpecies(UpsertCatalogItemDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.Species.AnyAsync(x => x.name == name);
        if (exists)
            return BadRequest("Species name already exists.");

        var entity = new Species { name = name };

        _context.Species.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSpeciesById), new { id = entity.id }, new
        {
            entity.id,
            entity.name
        });
    }

    [HttpPut("species/{id:int}")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<ActionResult> UpdateSpecies(int id, UpsertCatalogItemDto dto)
    {
        var entity = await _context.Species.FindAsync(id);

        if (entity == null)
            return NotFound("Species not found.");

        var name = dto.name.Trim();

        var exists = await _context.Species.AnyAsync(x => x.name == name && x.id != id);
        if (exists)
            return BadRequest("Species name already exists.");

        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Species updated successfully.", id = entity.id });
    }

    [HttpDelete("species/{id:int}")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<ActionResult> DeleteSpecies(int id)
    {
        var entity = await _context.Species.FindAsync(id);

        if (entity == null)
            return NotFound("Species not found.");

        _context.Species.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this species because it is being used.");
        }

        return Ok(new { message = "Species deleted successfully.", id });
    }

    // =========================
    // BREEDS
    // =========================

    [HttpGet("breeds")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BreedCatalogDto>>> GetBreeds()
    {
        var data = await _context.Breeds
            .AsNoTracking()
            .OrderBy(x => x.species.name)
            .ThenBy(x => x.name)
            .Select(x => new BreedCatalogDto
            {
                id = x.id,
                name = x.name,
                speciesId = x.speciesId,
                speciesName = x.species.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("breeds/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<BreedCatalogDto>> GetBreedById(int id)
    {
        var item = await _context.Breeds
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new BreedCatalogDto
            {
                id = x.id,
                name = x.name,
                speciesId = x.speciesId,
                speciesName = x.species.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Breed not found.");

        return Ok(item);
    }

    [HttpGet("breeds/by-species/{speciesId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BreedCatalogDto>>> GetBreedsBySpecies(int speciesId)
    {
        var speciesExists = await _context.Species.AnyAsync(x => x.id == speciesId);

        if (!speciesExists)
            return NotFound("Species not found.");

        var data = await _context.Breeds
            .AsNoTracking()
            .Where(x => x.speciesId == speciesId)
            .OrderBy(x => x.name)
            .Select(x => new BreedCatalogDto
            {
                id = x.id,
                name = x.name,
                speciesId = x.speciesId,
                speciesName = x.species.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("breeds")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult> CreateBreed(UpsertBreedDto dto)
    {
        var name = dto.name.Trim();

        var speciesExists = await _context.Species.AnyAsync(x => x.id == dto.speciesId);
        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var exists = await _context.Breeds.AnyAsync(x =>
            x.speciesId == dto.speciesId &&
            x.name == name
        );

        if (exists)
            return BadRequest("Breed already exists for this species.");

        var entity = new Breed
        {
            speciesId = dto.speciesId,
            name = name
        };

        _context.Breeds.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBreedById), new { id = entity.id }, new
        {
            entity.id,
            entity.name,
            entity.speciesId
        });
    }

    [HttpPut("breeds/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> UpdateBreed(int id, UpsertBreedDto dto)
    {
        var entity = await _context.Breeds.FindAsync(id);

        if (entity == null)
            return NotFound("Breed not found.");

        var name = dto.name.Trim();

        var speciesExists = await _context.Species.AnyAsync(x => x.id == dto.speciesId);
        if (!speciesExists)
            return BadRequest("Invalid speciesId.");

        var exists = await _context.Breeds.AnyAsync(x =>
            x.id != id &&
            x.speciesId == dto.speciesId &&
            x.name == name
        );

        if (exists)
            return BadRequest("Breed already exists for this species.");

        entity.speciesId = dto.speciesId;
        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Breed updated successfully.", id = entity.id });
    }

    [HttpDelete("breeds/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> DeleteBreed(int id)
    {
        var entity = await _context.Breeds.FindAsync(id);

        if (entity == null)
            return NotFound("Breed not found.");

        _context.Breeds.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this breed because it is being used.");
        }

        return Ok(new { message = "Breed deleted successfully.", id });
    }

    // =========================
    // SIZES
    // =========================

    [HttpGet("sizes")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetSizes()
    {
        var data = await _context.Sizes
            .AsNoTracking()
            .OrderBy(x => x.id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("sizes/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CatalogItemDto>> GetSizeById(int id)
    {
        var item = await _context.Sizes
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Size not found.");

        return Ok(item);
    }

    [HttpPost("sizes")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> CreateSize(UpsertCatalogItemDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.Sizes.AnyAsync(x => x.name == name);
        if (exists)
            return BadRequest("Size name already exists.");

        var entity = new Size { name = name };

        _context.Sizes.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSizeById), new { id = entity.id }, new
        {
            entity.id,
            entity.name
        });
    }

    [HttpPut("sizes/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> UpdateSize(int id, UpsertCatalogItemDto dto)
    {
        var entity = await _context.Sizes.FindAsync(id);

        if (entity == null)
            return NotFound("Size not found.");

        var name = dto.name.Trim();

        var exists = await _context.Sizes.AnyAsync(x => x.name == name && x.id != id);
        if (exists)
            return BadRequest("Size name already exists.");

        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Size updated successfully.", id = entity.id });
    }

    [HttpDelete("sizes/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> DeleteSize(int id)
    {
        var entity = await _context.Sizes.FindAsync(id);

        if (entity == null)
            return NotFound("Size not found.");

        _context.Sizes.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this size because it is being used.");
        }

        return Ok(new { message = "Size deleted successfully.", id });
    }

    // =========================
    // PET STATUSES
    // =========================

    [HttpGet("pet-statuses")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetPetStatuses()
    {
        var data = await _context.PetStatuses
            .AsNoTracking()
            .OrderBy(x => x.id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("pet-statuses/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CatalogItemDto>> GetPetStatusById(int id)
    {
        var item = await _context.PetStatuses
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Pet status not found.");

        return Ok(item);
    }

    [HttpPost("pet-statuses")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> CreatePetStatus(UpsertCatalogItemDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.PetStatuses.AnyAsync(x => x.name == name);
        if (exists)
            return BadRequest("Pet status name already exists.");

        var entity = new PetStatus { name = name };

        _context.PetStatuses.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPetStatusById), new { id = entity.id }, new
        {
            entity.id,
            entity.name
        });
    }

    [HttpPut("pet-statuses/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> UpdatePetStatus(int id, UpsertCatalogItemDto dto)
    {
        var entity = await _context.PetStatuses.FindAsync(id);

        if (entity == null)
            return NotFound("Pet status not found.");

        var name = dto.name.Trim();

        var exists = await _context.PetStatuses.AnyAsync(x => x.name == name && x.id != id);
        if (exists)
            return BadRequest("Pet status name already exists.");

        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Pet status updated successfully.", id = entity.id });
    }

    [HttpDelete("pet-statuses/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> DeletePetStatus(int id)
    {
        var entity = await _context.PetStatuses.FindAsync(id);

        if (entity == null)
            return NotFound("Pet status not found.");

        _context.PetStatuses.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this pet status because it is being used.");
        }

        return Ok(new { message = "Pet status deleted successfully.", id });
    }

    // =========================
    // REQUEST STATUSES
    // =========================

    [HttpGet("request-statuses")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetRequestStatuses()
    {
        var data = await _context.RequestStatuses
            .AsNoTracking()
            .OrderBy(x => x.id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("request-statuses/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult<CatalogItemDto>> GetRequestStatusById(int id)
    {
        var item = await _context.RequestStatuses
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Request status not found.");

        return Ok(item);
    }

    [HttpPost("request-statuses")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> CreateRequestStatus(UpsertCatalogItemDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.RequestStatuses.AnyAsync(x => x.name == name);
        if (exists)
            return BadRequest("Request status name already exists.");

        var entity = new RequestStatus { name = name };

        _context.RequestStatuses.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRequestStatusById), new { id = entity.id }, new
        {
            entity.id,
            entity.name
        });
    }

    [HttpPut("request-statuses/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> UpdateRequestStatus(int id, UpsertCatalogItemDto dto)
    {
        var entity = await _context.RequestStatuses.FindAsync(id);

        if (entity == null)
            return NotFound("Request status not found.");

        var name = dto.name.Trim();

        var exists = await _context.RequestStatuses.AnyAsync(x => x.name == name && x.id != id);
        if (exists)
            return BadRequest("Request status name already exists.");

        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Request status updated successfully.", id = entity.id });
    }

    [HttpDelete("request-statuses/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> DeleteRequestStatus(int id)
    {
        var entity = await _context.RequestStatuses.FindAsync(id);

        if (entity == null)
            return NotFound("Request status not found.");

        _context.RequestStatuses.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this request status because it is being used.");
        }

        return Ok(new { message = "Request status deleted successfully.", id });
    }

    // =========================
    // ROLES
    // =========================

    [HttpGet("roles")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetRoles()
    {
        var data = await _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("roles/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult<CatalogItemDto>> GetRoleById(int id)
    {
        var item = await _context.Roles
            .AsNoTracking()
            .Where(x => x.id == id)
            .Select(x => new CatalogItemDto
            {
                id = x.id,
                name = x.name
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Role not found.");

        return Ok(item);
    }

    [HttpPost("roles")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> CreateRole(UpsertCatalogItemDto dto)
    {
        var name = dto.name.Trim();

        var exists = await _context.Roles.AnyAsync(x => x.name == name);
        if (exists)
            return BadRequest("Role name already exists.");

        var entity = new Role { name = name };

        _context.Roles.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoleById), new { id = entity.id }, new
        {
            entity.id,
            entity.name
        });
    }

    [HttpPut("roles/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> UpdateRole(int id, UpsertCatalogItemDto dto)
    {
        var entity = await _context.Roles.FindAsync(id);

        if (entity == null)
            return NotFound("Role not found.");

        var name = dto.name.Trim();

        var exists = await _context.Roles.AnyAsync(x => x.name == name && x.id != id);
        if (exists)
            return BadRequest("Role name already exists.");

        entity.name = name;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Role updated successfully.", id = entity.id });
    }

    [HttpDelete("roles/{id:int}")]
    [Authorize(Roles = "Admin , Administrador")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        var entity = await _context.Roles.FindAsync(id);

        if (entity == null)
            return NotFound("Role not found.");

        _context.Roles.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this role because it is being used.");
        }

        return Ok(new { message = "Role deleted successfully.", id });
    }
}