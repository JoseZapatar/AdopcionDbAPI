using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.AdoptionRequests;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdoptionRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdoptionRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AdoptionRequestDto>>> GetAdoptionRequests(
        [FromQuery] int? adopterId,
        [FromQuery] int? petId,
        [FromQuery] int? statusId
    )
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .AsQueryable();

        if (adopterId.HasValue)
            query = query.Where(r => r.adopterId == adopterId.Value);

        if (petId.HasValue)
            query = query.Where(r => r.petId == petId.Value);

        if (statusId.HasValue)
            query = query.Where(r => r.statusId == statusId.Value);

        var requests = await query
            .OrderByDescending(r => r.createdAt)
            .Select(r => new AdoptionRequestDto
            {
                id = r.id,

                adopterId = r.adopterId,
                adopterName = r.adopter.user.name,
                adopterEmail = r.adopter.user.email,
                adopterPhone = r.adopter.phone,
                adopterCity = r.adopter.city,

                petId = r.petId,
                petName = r.pet.name,
                speciesName = r.pet.species.name,
                breedName = r.pet.breed != null ? r.pet.breed.name : null,

                statusId = r.statusId,
                requestStatus = r.status.name,

                message = r.message,
                createdAt = r.createdAt,
                updatedAt = r.updatedAt,

                reviewedByUserId = r.reviewedByUserId,
                reviewedByUserName = r.reviewedByUser != null ? r.reviewedByUser.name : null,
                reviewedAt = r.reviewedAt,
                decisionNotes = r.decisionNotes
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<AdoptionRequestDto>> GetAdoptionRequest(int id)
    {
        var request = await _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.id == id)
            .Select(r => new AdoptionRequestDto
            {
                id = r.id,

                adopterId = r.adopterId,
                adopterName = r.adopter.user.name,
                adopterEmail = r.adopter.user.email,
                adopterPhone = r.adopter.phone,
                adopterCity = r.adopter.city,

                petId = r.petId,
                petName = r.pet.name,
                speciesName = r.pet.species.name,
                breedName = r.pet.breed != null ? r.pet.breed.name : null,

                statusId = r.statusId,
                requestStatus = r.status.name,

                message = r.message,
                createdAt = r.createdAt,
                updatedAt = r.updatedAt,

                reviewedByUserId = r.reviewedByUserId,
                reviewedByUserName = r.reviewedByUser != null ? r.reviewedByUser.name : null,
                reviewedAt = r.reviewedAt,
                decisionNotes = r.decisionNotes
            })
            .FirstOrDefaultAsync();

        if (request == null)
            return NotFound("Adoption request not found.");

        return Ok(request);
    }

    [HttpPost]
    [Authorize(Roles = "Adopter,Adoptante")]
    public async Task<ActionResult> CreateAdoptionRequest(CreateAdoptionRequestDto dto)
    {
        var pendingStatusId = await GetRequestStatusId("Pendiente");

        if (pendingStatusId == null)
            return BadRequest("Pending request status was not found.");

        var adopterExists = await _context.Adopters
            .AnyAsync(a => a.id == dto.adopterId);

        if (!adopterExists)
            return BadRequest("Invalid adopterId.");

        var pet = await _context.Pets
            .Include(p => p.status)
            .FirstOrDefaultAsync(p => p.id == dto.petId);

        if (pet == null)
            return BadRequest("Invalid petId.");

        if (pet.status.name != "Disponible")
            return BadRequest("This pet is not available for adoption.");

        var alreadyPending = await _context.AdoptionRequests
            .AnyAsync(r =>
                r.adopterId == dto.adopterId &&
                r.petId == dto.petId &&
                r.statusId == pendingStatusId.Value
            );

        if (alreadyPending)
            return BadRequest("This adopter already has a pending request for this pet.");

        var request = new AdoptionRequest
        {
            adopterId = dto.adopterId,
            petId = dto.petId,
            message = dto.message,
            statusId = pendingStatusId.Value,
            createdAt = DateTime.UtcNow
        };

        _context.AdoptionRequests.Add(request);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("The adoption request could not be created. Check duplicate or invalid data.");
        }

        return CreatedAtAction(nameof(GetAdoptionRequest), new { id = request.id }, new
        {
            message = "Adoption request created successfully.",
            requestId = request.id
        });
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ApproveAdoptionRequest(int id, ReviewAdoptionRequestDto dto)
    {
        var pendingStatusId = await GetRequestStatusId("Pendiente");
        var approvedStatusId = await GetRequestStatusId("Aprobada");

        if (pendingStatusId == null || approvedStatusId == null)
            return BadRequest("Required request statuses were not found.");

        var reviewerExists = await _context.Users
            .AnyAsync(u => u.id == dto.reviewedByUserId);

        if (!reviewerExists)
            return BadRequest("Invalid reviewedByUserId.");

        var request = await _context.AdoptionRequests
            .FirstOrDefaultAsync(r => r.id == id);

        if (request == null)
            return NotFound("Adoption request not found.");

        if (request.statusId != pendingStatusId.Value)
            return BadRequest("Only pending requests can be approved.");

        request.statusId = approvedStatusId.Value;
        request.reviewedByUserId = dto.reviewedByUserId;
        request.reviewedAt = DateTime.UtcNow;
        request.updatedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud aprobada."
            : dto.decisionNotes.Trim();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("The request could not be approved. The pet may already have an approved request.");
        }

        return Ok(new
        {
            message = "Adoption request approved successfully.",
            requestId = request.id
        });
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RejectAdoptionRequest(int id, ReviewAdoptionRequestDto dto)
    {
        var pendingStatusId = await GetRequestStatusId("Pendiente");
        var rejectedStatusId = await GetRequestStatusId("Rechazada");

        if (pendingStatusId == null || rejectedStatusId == null)
            return BadRequest("Required request statuses were not found.");

        var reviewerExists = await _context.Users
            .AnyAsync(u => u.id == dto.reviewedByUserId);

        if (!reviewerExists)
            return BadRequest("Invalid reviewedByUserId.");

        var request = await _context.AdoptionRequests
            .FirstOrDefaultAsync(r => r.id == id);

        if (request == null)
            return NotFound("Adoption request not found.");

        if (request.statusId != pendingStatusId.Value)
            return BadRequest("Only pending requests can be rejected.");

        request.statusId = rejectedStatusId.Value;
        request.reviewedByUserId = dto.reviewedByUserId;
        request.reviewedAt = DateTime.UtcNow;
        request.updatedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud rechazada."
            : dto.decisionNotes.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Adoption request rejected successfully.",
            requestId = request.id
        });
    }

    private async Task<int?> GetRequestStatusId(string statusName)
    {
        return await _context.RequestStatuses
            .Where(s => s.name == statusName)
            .Select(s => (int?)s.id)
            .FirstOrDefaultAsync();
    }
}