using AdopcionDbAPI.Context;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RecommendationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRecommendation(CreateRecommendationDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.id == userId.Value);

        if (user == null)
            return Unauthorized("Invalid user.");

        var message = dto.message?.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return BadRequest("Recommendation message is required.");

        if (message.Length < 10)
            return BadRequest("Recommendation must have at least 10 characters.");

        var recommendation = new Recommendation
        {
            userId = user.id,
            name = user.name,
            email = user.email,
            message = message,
            status = "Pendiente",
            createdAt = DateTime.UtcNow
        };

        _context.Recommendations.Add(recommendation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Recommendation sent successfully.",
            id = recommendation.id
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> GetRecommendations([FromQuery] string? status)
    {
        var query = _context.Recommendations
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(r => r.status.ToLower() == normalizedStatus);
        }

        var recommendations = await query
            .OrderByDescending(r => r.createdAt)
            .Select(r => new
            {
                id = r.id,
                userId = r.userId,
                name = r.name,
                email = r.email,
                message = r.message,
                status = r.status,
                adminNotes = r.adminNotes,
                createdAt = r.createdAt,
                reviewedAt = r.reviewedAt
            })
            .ToListAsync();

        return Ok(recommendations);
    }

    [HttpPut("{id:int}/review")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> ReviewRecommendation(int id, ReviewRecommendationDto dto)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);

        if (recommendation == null)
            return NotFound("Recommendation not found.");

        var status = dto.status?.Trim();

        if (string.IsNullOrWhiteSpace(status))
            return BadRequest("Status is required.");

        var allowedStatuses = new[] { "Pendiente", "Revisada", "Descartada" };

        if (!allowedStatuses.Any(s => s.Equals(status, StringComparison.OrdinalIgnoreCase)))
            return BadRequest("Invalid recommendation status.");

        recommendation.status = allowedStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        recommendation.adminNotes = string.IsNullOrWhiteSpace(dto.adminNotes) ? null : dto.adminNotes.Trim();
        recommendation.reviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Recommendation updated successfully.",
            id = recommendation.id
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Administrador")]
    public async Task<IActionResult> DeleteRecommendation(int id)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);

        if (recommendation == null)
            return NotFound("Recommendation not found.");

        _context.Recommendations.Remove(recommendation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Recommendation deleted successfully.",
            id
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return userId;
    }
}

public class CreateRecommendationDto
{
    public string message { get; set; } = string.Empty;
}

public class ReviewRecommendationDto
{
    public string status { get; set; } = "Revisada";

    public string? adminNotes { get; set; }
}
