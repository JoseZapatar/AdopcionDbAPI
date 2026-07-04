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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromForm] RegisterDto dto)
    {
        var email = dto.email.Trim().ToLower();

        var emailExists = await _context.Users
            .AnyAsync(u => u.email.ToLower() == email);

        if (emailExists)
            return BadRequest("Email already exists.");

        var accountType = dto.accountType?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(accountType))
            accountType = "adoptar";

        if (accountType != "adoptar" && accountType != "dar")
            return BadRequest("Invalid account type.");

        var wantsToAdopt = accountType == "adoptar";

        var selectedRole = await GetOrCreateRole("Adoptante");

        if (selectedRole == null)
        {
            return BadRequest("Default adoptante role was not found.");
        }

        var user = new User
        {
            name = dto.name.Trim(),
            email = email,
            roleId = selectedRole.id,
            passwordHash = "",
            createdAt = DateTime.UtcNow
        };

        user.passwordHash = _passwordHasher.HashPassword(user, dto.password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (wantsToAdopt)
        {
            var adopter = new Adopter
            {
                userId = user.id,
                phone = dto.phone,
                address = dto.address,
                city = dto.city,
                housingType = dto.housingType,
                hasOtherPets = dto.hasOtherPets,
                createdAt = DateTime.UtcNow
            };

            _context.Adopters.Add(adopter);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!dto.acceptsResponsibility)
                return BadRequest("Debes aceptar la responsabilidad de publicar informacion real.");

            if (dto.identificationImage == null || dto.identificationImage.Length == 0)
                return BadRequest("La foto de cedula es obligatoria para solicitar cuenta de publicador.");

            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

            if (!allowedContentTypes.Contains(dto.identificationImage.ContentType))
                return BadRequest("La cedula debe ser una imagen JPEG, PNG o WEBP.");

            const long maxFileSize = 5 * 1024 * 1024;

            if (dto.identificationImage.Length > maxFileSize)
                return BadRequest("La imagen de cedula no puede exceder 5 MB.");

            using var memoryStream = new MemoryStream();
            await dto.identificationImage.CopyToAsync(memoryStream);

            var publisherRequest = new PublisherRequest
            {
                userId = user.id,
                status = "Pendiente",
                requestedAt = DateTime.UtcNow,
                decisionNotes = BuildPublisherRequestNotes(dto),
                identificationImageData = memoryStream.ToArray(),
                identificationImageContentType = dto.identificationImage.ContentType
            };

            _context.PublisherRequests.Add(publisherRequest);
            await _context.SaveChangesAsync();
        }

        user.role = selectedRole;

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var email = dto.email.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.role)
            .FirstOrDefaultAsync(u => u.email.ToLower() == email);

        if (user == null)
            return Unauthorized("Invalid email or password.");

        PasswordVerificationResult result;

        try
        {
            result = _passwordHasher.VerifyHashedPassword(
                user,
                user.passwordHash,
                dto.password
            );
        }
        catch (FormatException)
        {
            return Unauthorized("Invalid email or password.");
        }

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        return Ok(CreateAuthResponse(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized("Invalid token.");

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.role)
            .Where(u => u.id == userId)
            .Select(u => new AuthUserDto
            {
                id = u.id,
                name = u.name,
                email = u.email,
                roleId = u.roleId,
                roleName = u.role.name
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
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

    private static string BuildPublisherRequestNotes(RegisterDto dto)
    {
        var notes = string.Join("\n", new[]
        {
            $"Nombre legal: {dto.legalName}",
            $"Telefono: {dto.phone}",
            $"Ciudad: {dto.city}",
            $"Direccion: {dto.address}",
            $"Tipos de mascotas: {dto.animalTypes}",
            $"Capacidad mensual: {dto.monthlyCapacity}",
            $"Experiencia: {dto.experience}",
            $"Espacio/refugio: {dto.facilityType}",
            $"Disponibilidad: {dto.availability}",
            $"Referencia: {dto.referenceContact}",
            $"Tiene transporte: {(dto.hasTransport ? "Si" : "No")}",
            $"Motivo: {dto.motivation}"
        });

        return notes.Length > 1000 ? notes[..1000] : notes;
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

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

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
