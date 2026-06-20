using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Reviews;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews()
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.createdAt)
            .Select(r => new ReviewDto
            {
                id = r.id,
                userId = r.userId,
                userName = r.user.name,
                petId = r.petId,
                petName = r.pet.name,
                rating = r.rating,
                comment = r.comment,
                createdAt = r.createdAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewDto>> GetReview(int id)
    {
        var review = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.id == id)
            .Select(r => new ReviewDto
            {
                id = r.id,
                userId = r.userId,
                userName = r.user.name,
                petId = r.petId,
                petName = r.pet.name,
                rating = r.rating,
                comment = r.comment,
                createdAt = r.createdAt
            })
            .FirstOrDefaultAsync();

        if (review == null)
            return NotFound("Review not found.");

        return Ok(review);
    }

    [HttpGet("pet/{petId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsByPet(int petId)
    {
        var petExists = await _context.Pets.AnyAsync(p => p.id == petId);

        if (!petExists)
            return NotFound("Pet not found.");

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.petId == petId)
            .OrderByDescending(r => r.createdAt)
            .Select(r => new ReviewDto
            {
                id = r.id,
                userId = r.userId,
                userName = r.user.name,
                petId = r.petId,
                petName = r.pet.name,
                rating = r.rating,
                comment = r.comment,
                createdAt = r.createdAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost]
    [Authorize(Roles = "Adopter,Adoptante")]
    public async Task<ActionResult> CreateReview(CreateReviewDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var petExists = await _context.Pets.AnyAsync(p => p.id == dto.petId);

        if (!petExists)
            return BadRequest("Invalid petId.");

        var approvedStatusId = await _context.RequestStatuses
            .Where(s => s.name == "Aprobada")
            .Select(s => (int?)s.id)
            .FirstOrDefaultAsync();

        if (approvedStatusId == null)
            return BadRequest("Approved request status was not found.");

        var userAdoptedPet = await _context.AdoptionRequests
            .AnyAsync(r =>
                r.petId == dto.petId &&
                r.statusId == approvedStatusId.Value &&
                r.adopter.userId == userId.Value
            );

        if (!userAdoptedPet)
            return BadRequest("Only users with an approved adoption request for this pet can review it.");

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.userId == userId.Value && r.petId == dto.petId);

        if (alreadyReviewed)
            return BadRequest("You already reviewed this pet.");

        var review = new Review
        {
            userId = userId.Value,
            petId = dto.petId,
            rating = dto.rating,
            comment = dto.comment,
            createdAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReview), new { id = review.id }, new
        {
            message = "Review created successfully.",
            reviewId = review.id
        });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult> UpdateReview(int id, UpdateReviewDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
            return NotFound("Review not found.");

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && review.userId != userId.Value)
            return Forbid();

        review.rating = dto.rating;
        review.comment = dto.comment;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Review updated successfully.",
            reviewId = review.id
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult> DeleteReview(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
            return NotFound("Review not found.");

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && review.userId != userId.Value)
            return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Review deleted successfully.",
            reviewId = id
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