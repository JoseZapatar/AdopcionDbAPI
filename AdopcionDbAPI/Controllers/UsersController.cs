using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.Users;
using AdopcionDbAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UsersController(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
        [FromQuery] int? roleId,
        [FromQuery] string? search
    )
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.role)
            .Include(u => u.Adopter)
            .AsQueryable();

        if (roleId.HasValue)
            query = query.Where(u => u.roleId == roleId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.name.Contains(search) ||
                u.email.Contains(search)
            );
        }

        var users = await query
            .OrderBy(u => u.name)
            .Select(u => new UserDto
            {
                id = u.id,
                name = u.name,
                email = u.email,
                roleId = u.roleId,
                roleName = u.role.name,
                createdAt = u.createdAt,
                hasAdopterProfile = u.Adopter != null
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.role)
            .Include(u => u.Adopter)
            .Where(u => u.id == id)
            .Select(u => new UserDto
            {
                id = u.id,
                name = u.name,
                email = u.email,
                roleId = u.roleId,
                roleName = u.role.name,
                createdAt = u.createdAt,
                hasAdopterProfile = u.Adopter != null
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser(CreateUserDto dto)
    {
        var email = dto.email.Trim().ToLower();

        var roleExists = await _context.Roles.AnyAsync(r => r.id == dto.roleId);
        if (!roleExists)
            return BadRequest("Invalid roleId.");

        var emailExists = await _context.Users.AnyAsync(u => u.email.ToLower() == email);
        if (emailExists)
            return BadRequest("Email already exists.");

        var user = new User
        {
            name = dto.name.Trim(),
            email = email,
            roleId = dto.roleId,
            passwordHash = "",
            createdAt = DateTime.UtcNow
        };

        user.passwordHash = _passwordHasher.HashPassword(user, dto.password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.id }, new
        {
            message = "User created successfully.",
            userId = user.id
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("User not found.");

        var email = dto.email.Trim().ToLower();

        var roleExists = await _context.Roles.AnyAsync(r => r.id == dto.roleId);
        if (!roleExists)
            return BadRequest("Invalid roleId.");

        var emailExists = await _context.Users.AnyAsync(u =>
            u.email.ToLower() == email &&
            u.id != id
        );

        if (emailExists)
            return BadRequest("Email already exists.");

        user.name = dto.name.Trim();
        user.email = email;
        user.roleId = dto.roleId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User updated successfully.",
            userId = user.id
        });
    }

    [HttpPut("{id:int}/password")]
    public async Task<ActionResult> ChangePassword(int id, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("User not found.");

        user.passwordHash = _passwordHasher.HashPassword(user, dto.newPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Password updated successfully.",
            userId = user.id
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("User not found.");

        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("Cannot delete this user because it is being used by other records.");
        }

        return Ok(new
        {
            message = "User deleted successfully.",
            userId = id
        });
    }
}