namespace AdopcionDbAPI.DTOs.AuditLogs;

public class AuditLogDto
{
    public int id { get; set; }

    public string tableName { get; set; } = null!;

    public string actionName { get; set; } = null!;

    public int? recordId { get; set; }

    public string userName { get; set; } = null!;

    public DateTime actionDate { get; set; }

    public string? details { get; set; }
}