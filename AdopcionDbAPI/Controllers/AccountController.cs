using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Auth;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    public AccountController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.role)
            .FirstOrDefaultAsync(u => u.id == userId.Value);

        if (user == null)
            return NotFound("User not found.");

        var adopter = await _context.Adopters
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.userId == user.id);

        return Ok(new
        {
            user = new AuthUserDto
            {
                id = user.id,
                name = user.name,
                email = user.email,
                roleId = user.roleId,
                roleName = user.role.name
            },
            adopter = adopter == null ? null : new
            {
                id = adopter.id,
                phone = adopter.phone,
                address = adopter.address,
                city = adopter.city,
                housingType = adopter.housingType,
                hasOtherPets = adopter.hasOtherPets
            }
        });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<AuthResponseDto>> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .Include(u => u.role)
            .FirstOrDefaultAsync(u => u.id == userId.Value);

        if (user == null)
            return NotFound("User not found.");

        var name = dto.name?.Trim();
        var email = dto.email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var emailExists = await _context.Users
            .AnyAsync(u => u.id != user.id && u.email.ToLower() == email);

        if (emailExists)
            return BadRequest("Email already exists.");

        user.name = name;
        user.email = email;

        await _context.SaveChangesAsync();

        return Ok(CreateAuthResponse(user));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.id == userId.Value);

        if (user == null)
            return NotFound("User not found.");

        if (string.IsNullOrWhiteSpace(dto.currentPassword))
            return BadRequest("Current password is required.");

        if (string.IsNullOrWhiteSpace(dto.newPassword) || dto.newPassword.Length < 6)
            return BadRequest("New password must have at least 6 characters.");

        PasswordVerificationResult result;

        try
        {
            result = _passwordHasher.VerifyHashedPassword(
                user,
                user.passwordHash,
                dto.currentPassword
            );
        }
        catch (FormatException)
        {
            return BadRequest("Current password is incorrect.");
        }

        if (result == PasswordVerificationResult.Failed)
            return BadRequest("Current password is incorrect.");

        user.passwordHash = _passwordHasher.HashPassword(user, dto.newPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }

    [HttpPut("switch-role")]
    public async Task<ActionResult<AuthResponseDto>> SwitchRole(SwitchAccountRoleDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .Include(u => u.role)
            .FirstOrDefaultAsync(u => u.id == userId.Value);

        if (user == null)
            return NotFound("User not found.");

        var currentRole = user.role.name.ToLower();

        if (currentRole == "admin" || currentRole == "administrador")
            return BadRequest("Administrator accounts cannot be changed from this page.");

        var accountType = dto.accountType?.Trim().ToLower();

        if (accountType != "adoptar" && accountType != "dar")
            return BadRequest("Invalid account type.");

        var targetRoleName = accountType == "adoptar" ? "adoptante" : "publicador";

        var targetRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.name.ToLower() == targetRoleName);

        if (targetRole == null)
            return BadRequest($"Role '{targetRoleName}' was not found.");

        user.roleId = targetRole.id;

        if (accountType == "adoptar")
        {
            var adopter = await _context.Adopters
                .FirstOrDefaultAsync(a => a.userId == user.id);

            if (adopter == null)
            {
                adopter = new Adopter
                {
                    userId = user.id,
                    phone = dto.phone,
                    address = dto.address,
                    city = dto.city,
                    housingType = string.IsNullOrWhiteSpace(dto.housingType) ? "casa" : dto.housingType,
                    hasOtherPets = dto.hasOtherPets,
                    createdAt = DateTime.UtcNow
                };

                _context.Adopters.Add(adopter);
            }
            else
            {
                adopter.phone = dto.phone;
                adopter.address = dto.address;
                adopter.city = dto.city;
                adopter.housingType = string.IsNullOrWhiteSpace(dto.housingType) ? adopter.housingType : dto.housingType;
                adopter.hasOtherPets = dto.hasOtherPets;
            }
        }

        await _context.SaveChangesAsync();

        user.role = targetRole;

        return Ok(CreateAuthResponse(user));
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return null;

        return userId;
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var expiresMinutes = int.Parse(_configuration["Jwt:ExpiresMinutes"] ?? "120");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);
        var token = GenerateJwtToken(user, expiresAt);

        return new AuthResponseDto
        {
            token = token,
            expiresAt = expiresAt,
            user = new AuthUserDto
            {
                id = user.id,
                name = user.name,
                email = user.email,
                roleId = user.roleId,
                roleName = user.role.name
            }
        };
    }

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new Exception("JWT Key is missing.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
            new Claim(ClaimTypes.Name, user.name),
            new Claim(ClaimTypes.Email, user.email),
            new Claim(ClaimTypes.Role, user.role.name),
            new Claim("roleId", user.roleId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class UpdateProfileDto
{
    public string name { get; set; } = string.Empty;

    public string email { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string currentPassword { get; set; } = string.Empty;

    public string newPassword { get; set; } = string.Empty;
}

public class SwitchAccountRoleDto
{
    public string accountType { get; set; } = "adoptar";

    public string? phone { get; set; }

    public string? address { get; set; }

    public string? city { get; set; }

    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }
}
