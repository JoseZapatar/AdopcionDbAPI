using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ViewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ViewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("adopters-profiles")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult<IEnumerable<vw_AdoptersProfile>>> GetAdoptersProfiles(
        [FromQuery] string? city,
        [FromQuery] string? search
    )
    {
        var query = _context.vw_AdoptersProfiles
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.city != null && x.city.Contains(city));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.adopterName.Contains(search) ||
                x.email.Contains(search)
            );
        }

        var data = await query
            .OrderBy(x => x.adopterName)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("adoption-request-details")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult<IEnumerable<vw_AdoptionRequestDetail>>> GetAdoptionRequestDetails(
        [FromQuery] string? status,
        [FromQuery] string? species,
        [FromQuery] string? city,
        [FromQuery] string? search
    )
    {
        var query = _context.vw_AdoptionRequestDetails
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.requestStatus.Contains(status));

        if (!string.IsNullOrWhiteSpace(species))
            query = query.Where(x => x.speciesName.Contains(species));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.city != null && x.city.Contains(city));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.adopterName.Contains(search) ||
                x.email.Contains(search) ||
                x.petName.Contains(search)
            );
        }

        var data = await query
            .OrderByDescending(x => x.createdAt)
            .ToListAsync();

        return Ok(data);
    }

    // ESTA ES LA IMPORTANTE PARA EL FRONTEND
    [HttpGet("available-pets")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<vw_AvailablePet>>> GetAvailablePets(
        [FromQuery] string? species,
        [FromQuery] string? breed,
        [FromQuery] string? size,
        [FromQuery] string? gender,
        [FromQuery] string? search
    )
    {
        var query = _context.vw_AvailablePets
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(species))
            query = query.Where(x => x.speciesName.Contains(species));

        if (!string.IsNullOrWhiteSpace(breed))
            query = query.Where(x => x.breedName != null && x.breedName.Contains(breed));

        if (!string.IsNullOrWhiteSpace(size))
            query = query.Where(x => x.sizeName.Contains(size));

        if (!string.IsNullOrWhiteSpace(gender))
            query = query.Where(x => x.gender != null && x.gender.Contains(gender));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.petName.Contains(search) ||
                (x.description != null && x.description.Contains(search))
            );
        }

        var data = await query
            .OrderByDescending(x => x.createdAt)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("executive-adoption-report")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult<IEnumerable<vw_ExecutiveAdoptionReport>>> GetExecutiveAdoptionReport()
    {
        var data = await _context.vw_ExecutiveAdoptionReports
            .AsNoTracking()
            .OrderBy(x => x.speciesName)
            .ThenBy(x => x.petStatus)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("pet-review-summary")]
    [Authorize(Roles = "Admin, Administrador")]
    public async Task<ActionResult<IEnumerable<vw_PetReviewSummary>>> GetPetReviewSummary(
        [FromQuery] string? search
    )
    {
        var query = _context.vw_PetReviewSummaries
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.petName.Contains(search));

        var data = await query
            .OrderByDescending(x => x.averageRating)
            .ThenByDescending(x => x.totalReviews)
            .ToListAsync();

        return Ok(data);
    }
}