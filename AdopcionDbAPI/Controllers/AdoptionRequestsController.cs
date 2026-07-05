using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.AdoptionRequests;
using AdopcionDbAPI.Models;
using AdopcionDbAPI.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdoptionRequestsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdoptionRequestsController> _logger;

    public AdoptionRequestsController(
        AppDbContext context,
        IEmailService emailService,
        ILogger<AdoptionRequestsController> logger
    )
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Administrador")]
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
    [AllowAnonymous]
    public async Task<IActionResult> CreateAdoptionRequest(CreateAdoptionRequestDto dto)
    {
        var pendingStatusId = await GetRequestStatusId("Pendiente");

        if (pendingStatusId == null)
            return BadRequest("Pending request status was not found.");

        var currentUserId = GetCurrentUserId();

        var pet = await _context.Pets
            .Include(p => p.status)
            .FirstOrDefaultAsync(p => p.id == dto.petId);

        if (pet == null)
            return BadRequest("Invalid petId.");

        if (!pet.status.name.Equals("disponible", StringComparison.OrdinalIgnoreCase))
            return BadRequest("This pet is not available for adoption.");

        Adopter? adopter;

        if (dto.adopterId.HasValue)
        {
            if (currentUserId == null)
                return Unauthorized("Invalid token.");

            adopter = await _context.Adopters
                .FirstOrDefaultAsync(a => a.id == dto.adopterId.Value && a.userId == currentUserId.Value);

            if (adopter == null)
                return BadRequest("Invalid adopterId.");
        }
        else
        {
            try
            {
                adopter = await GetOrCreatePublicAdopter(dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        var alreadyPending = await _context.AdoptionRequests
            .AnyAsync(r =>
                r.adopterId == adopter.id &&
                r.petId == dto.petId &&
                r.statusId == pendingStatusId.Value
            );

        if (alreadyPending)
            return BadRequest("This adopter already has a pending request for this pet.");

        var request = new AdoptionRequest
        {
            adopterId = adopter.id,
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

    private async Task<Adopter> GetOrCreatePublicAdopter(CreateAdoptionRequestDto dto)
    {
        var name = dto.adopterName?.Trim();
        var email = dto.adopterEmail?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Adopter name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Adopter email is required.");

        var user = await _context.Users
            .Include(u => u.Adopter)
            .FirstOrDefaultAsync(u => u.email.ToLower() == email);

        if (user == null)
        {
            var adopterRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.name.ToLower() == "adoptante" || r.name.ToLower() == "adopter");

            if (adopterRole == null)
                throw new InvalidOperationException("Default adopter role was not found.");

            user = new User
            {
                name = name,
                email = email,
                roleId = adopterRole.id,
                passwordHash = "PUBLIC_ADOPTER_NO_PASSWORD",
                createdAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        if (user.Adopter != null)
        {
            user.Adopter.phone = dto.adopterPhone ?? user.Adopter.phone;
            user.Adopter.address = dto.address ?? user.Adopter.address;
            user.Adopter.city = dto.adopterCity ?? user.Adopter.city;
            user.Adopter.housingType = dto.housingType ?? user.Adopter.housingType;
            user.Adopter.hasOtherPets = dto.hasOtherPets;
            user.Adopter.updatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return user.Adopter;
        }

        var adopter = new Adopter
        {
            userId = user.id,
            phone = dto.adopterPhone,
            address = dto.address,
            city = dto.adopterCity,
            housingType = dto.housingType,
            hasOtherPets = dto.hasOtherPets,
            createdAt = DateTime.UtcNow
        };

        _context.Adopters.Add(adopter);
        await _context.SaveChangesAsync();

        return adopter;
    }

    [HttpGet("my-requests/{adopterId:int}")]
    [Authorize(Roles = "Adopter,Adoptante")]
    public async Task<IActionResult> GetMyRequests(int adopterId)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized("Invalid token.");

        var ownsAdopter = await _context.Adopters
            .AnyAsync(a => a.id == adopterId && a.userId == currentUserId.Value);

        if (!ownsAdopter)
            return Forbid();

        var requests = await _context.AdoptionRequests
            .Where(r => r.adopterId == adopterId)
            .OrderByDescending(r => r.createdAt)
            .Select(r => new
            {
                id = r.id,
                petId = r.petId,
                petName = r.pet.name,
                status = r.status.name,
                message = r.message,
                decisionNotes = r.decisionNotes,
                createdAt = r.createdAt,
                updatedAt = r.updatedAt,
                primaryImageId = r.pet.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("my-history/{adopterId:int}")]
    [Authorize(Roles = "Adopter,Adoptante")]
    public async Task<IActionResult> GetMyHistory(int adopterId)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized("Invalid token.");

        var ownsAdopter = await _context.Adopters
            .AnyAsync(a => a.id == adopterId && a.userId == currentUserId.Value);

        if (!ownsAdopter)
            return Forbid();

        var history = await _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.adopterId == adopterId)
            .Where(r => r.status.name.ToLower() == "aprobada")
            .OrderByDescending(r => r.reviewedAt ?? r.updatedAt ?? r.createdAt)
            .Select(r => new
            {
                id = r.id,
                petId = r.petId,
                petName = r.pet.name,
                speciesName = r.pet.species.name,
                breedName = r.pet.breed != null ? r.pet.breed.name : null,
                message = r.message,
                decisionNotes = r.decisionNotes,
                adoptedAt = r.reviewedAt ?? r.updatedAt ?? r.createdAt,
                publisherUserId = r.pet.publisherUserId,
                publisherName = r.pet.publisherUser != null ? r.pet.publisherUser.name : null,
                primaryImageId = r.pet.PetImages
                    .Where(i => i.isPrimary)
                    .Select(i => (int?)i.id)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> ApproveAdoptionRequest(int id, ReviewAdoptionRequestDto dto)
    {
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
            return BadRequest("Pet status 'adoptado' was not found.");

        var reviewerExists = await _context.Users
            .AnyAsync(u => u.id == dto.reviewedByUserId);

        if (!reviewerExists)
            return BadRequest("Invalid reviewedByUserId.");

        var request = await _context.AdoptionRequests
            .Include(r => r.pet)
            .Include(r => r.adopter)
                .ThenInclude(a => a.user)
            .FirstOrDefaultAsync(r => r.id == id);

        if (request == null)
            return NotFound("Adoption request not found.");

        if (request.statusId != pendingStatusId.Value)
            return BadRequest("Only pending requests can be approved.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        request.statusId = approvedStatusId.Value;
        request.reviewedByUserId = dto.reviewedByUserId;
        request.reviewedAt = DateTime.UtcNow;
        request.updatedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud aprobada."
            : dto.decisionNotes.Trim();

        request.pet.statusId = adoptedPetStatusId.Value;
        request.pet.updatedAt = DateTime.UtcNow;

        var otherPendingRequests = await _context.AdoptionRequests
            .Include(r => r.pet)
            .Include(r => r.adopter)
                .ThenInclude(a => a.user)
            .Where(r =>
                r.petId == request.petId &&
                r.id != request.id &&
                r.statusId == pendingStatusId.Value
            )
            .ToListAsync();

        foreach (var otherRequest in otherPendingRequests)
        {
            otherRequest.statusId = cancelledStatusId.Value;
            otherRequest.reviewedByUserId = dto.reviewedByUserId;
            otherRequest.reviewedAt = DateTime.UtcNow;
            otherRequest.updatedAt = DateTime.UtcNow;
            otherRequest.decisionNotes = "Solicitud cancelada porque la mascota ya fue adoptada.";
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await SendAdoptionDecisionEmailAsync(
            request,
            "aprobada",
            request.decisionNotes
        );

        foreach (var otherRequest in otherPendingRequests)
        {
            await SendAdoptionDecisionEmailAsync(
                otherRequest,
                "cancelada",
                otherRequest.decisionNotes
            );
        }

        return Ok(new
        {
            message = "Adoption request approved successfully.",
            requestId = request.id,
            petId = request.petId,
            cancelledRequests = otherPendingRequests.Count
        });
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> RejectAdoptionRequest(int id, ReviewAdoptionRequestDto dto)
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
            .Include(r => r.pet)
            .Include(r => r.adopter)
                .ThenInclude(a => a.user)
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

        await SendAdoptionDecisionEmailAsync(
            request,
            "rechazada",
            request.decisionNotes
        );

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

    private async Task SendAdoptionDecisionEmailAsync(
        AdoptionRequest request,
        string decisionStatus,
        string? decisionNotes
    )
    {
        var recipientEmail = request.adopter.user.email;

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return;

        var petName = request.pet?.name ?? "la mascota solicitada";
        var adopterName = request.adopter.user.name;
        var notes = string.IsNullOrWhiteSpace(decisionNotes)
            ? "No se agregaron notas adicionales."
            : decisionNotes.Trim();

        var subject = decisionStatus switch
        {
            "aprobada" => $"Tu solicitud de adopcion para {petName} fue aprobada",
            "rechazada" => $"Tu solicitud de adopcion para {petName} fue rechazada",
            "cancelada" => $"Tu solicitud de adopcion para {petName} fue cancelada",
            _ => $"Actualizacion de tu solicitud de adopcion para {petName}"
        };

        var body = string.Join(Environment.NewLine, new[]
        {
            $"Hola {adopterName},",
            "",
            $"Tu solicitud de adopcion para {petName} fue {decisionStatus}.",
            "",
            $"Notas: {notes}",
            "",
            "Gracias por usar PetAdopt."
        });

        try
        {
            await _emailService.SendAsync(new EmailMessage(
                recipientEmail,
                subject,
                body
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not send adoption decision email for request {RequestId}.",
                request.id
            );
        }
    }
}
