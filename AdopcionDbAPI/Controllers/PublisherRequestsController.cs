using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Administrador,Revisor de publicadores,Revisor publicadores,Publisher Reviewer,PublisherReviewer")]
public class PublisherRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublisherRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests([FromQuery] string? status = "Pendiente")
    {
        var query = _context.PublisherRequests
            .AsNoTracking()
            .Include(r => r.user)
            .Include(r => r.reviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(r => r.status.ToLower() == normalizedStatus);
        }

        var requests = await query
            .OrderByDescending(r => r.requestedAt)
            .Select(r => new PublisherRequestDto
            {
                id = r.id,
                userId = r.userId,
                name = r.user.name,
                email = r.user.email,
                status = r.status,
                requestedAt = r.requestedAt,
                reviewedAt = r.reviewedAt,
                reviewedByUserId = r.reviewedByUserId,
                reviewedByUserName = r.reviewedByUser != null ? r.reviewedByUser.name : null,
                decisionNotes = r.decisionNotes,
                hasIdentificationImage = r.identificationImageData != null
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("{id:int}/identification")]
    public async Task<IActionResult> GetIdentificationImage(int id)
    {
        var request = await _context.PublisherRequests
            .AsNoTracking()
            .Where(r => r.id == id)
            .Select(r => new
            {
                r.identificationImageData,
                r.identificationImageContentType
            })
            .FirstOrDefaultAsync();

        if (request == null)
            return NotFound("Publisher request not found.");

        return File(request.identificationImageData, request.identificationImageContentType);
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, ReviewPublisherRequestDto dto)
    {
        var reviewerId = GetCurrentUserId();

        if (reviewerId == null)
            return Unauthorized("Invalid token.");

        var request = await _context.PublisherRequests
            .Include(r => r.user)
            .FirstOrDefaultAsync(r => r.id == id);

        if (request == null)
            return NotFound("Publisher request not found.");

        if (!request.status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only pending publisher requests can be approved.");

        var publisherRole = await GetOrCreateRole("Publicador");

        request.status = "Aprobada";
        request.reviewedByUserId = reviewerId.Value;
        request.reviewedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud de publicador aprobada."
            : dto.decisionNotes.Trim();

        request.user.roleId = publisherRole.id;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Publisher request approved successfully.",
            requestId = request.id,
            userId = request.userId,
            email = request.user.email
        });
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, ReviewPublisherRequestDto dto)
    {
        var reviewerId = GetCurrentUserId();

        if (reviewerId == null)
            return Unauthorized("Invalid token.");

        var request = await _context.PublisherRequests
            .Include(r => r.user)
            .FirstOrDefaultAsync(r => r.id == id);

        if (request == null)
            return NotFound("Publisher request not found.");

        if (!request.status.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only pending publisher requests can be rejected.");

        request.status = "Rechazada";
        request.reviewedByUserId = reviewerId.Value;
        request.reviewedAt = DateTime.UtcNow;
        request.decisionNotes = string.IsNullOrWhiteSpace(dto.decisionNotes)
            ? "Solicitud de publicador rechazada."
            : dto.decisionNotes.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Publisher request rejected successfully.",
            requestId = request.id,
            userId = request.userId,
            email = request.user.email
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return userId;
    }

    private async Task<Role> GetOrCreateRole(string name)
    {
        var normalized = name.ToLower();

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.name.ToLower() == normalized);

        if (role != null)
            return role;

        role = new Role { name = name };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }
}

public class PublisherRequestDto
{
    public int id { get; set; }

    public int userId { get; set; }

    public string name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string status { get; set; } = null!;

    public DateTime requestedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    public int? reviewedByUserId { get; set; }

    public string? reviewedByUserName { get; set; }

    public string? decisionNotes { get; set; }

    public bool hasIdentificationImage { get; set; }
}

public class ReviewPublisherRequestDto
{
    public string? decisionNotes { get; set; }
}
