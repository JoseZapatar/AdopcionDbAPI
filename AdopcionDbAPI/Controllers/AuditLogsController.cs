using AdopcionDbAPI.Context;
using AdopcionDbAPI.DTOs.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? tableName,
        [FromQuery] string? actionName,
        [FromQuery] string? userName,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate
    )
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            query = query.Where(a => a.tableName.Contains(tableName));
        }

        if (!string.IsNullOrWhiteSpace(actionName))
        {
            query = query.Where(a => a.actionName.Contains(actionName));
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(a => a.userName.Contains(userName));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.actionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.actionDate <= toDate.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.actionDate)
            .Select(a => new AuditLogDto
            {
                id = a.id,
                tableName = a.tableName,
                actionName = a.actionName,
                recordId = a.recordId,
                userName = a.userName,
                actionDate = a.actionDate,
                details = a.details
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(int id)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.id == id)
            .Select(a => new AuditLogDto
            {
                id = a.id,
                tableName = a.tableName,
                actionName = a.actionName,
                recordId = a.recordId,
                userName = a.userName,
                actionDate = a.actionDate,
                details = a.details
            })
            .FirstOrDefaultAsync();

        if (log == null)
            return NotFound("Audit log not found.");

        return Ok(log);
    }
}