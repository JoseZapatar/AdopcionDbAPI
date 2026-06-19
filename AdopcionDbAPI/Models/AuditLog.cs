using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

public partial class AuditLog
{
    [Key]
    public int id { get; set; }

    [StringLength(128)]
    public string tableName { get; set; } = null!;

    [StringLength(30)]
    public string actionName { get; set; } = null!;

    public int? recordId { get; set; }

    [StringLength(128)]
    public string userName { get; set; } = null!;

    public DateTime actionDate { get; set; }

    [StringLength(1000)]
    public string? details { get; set; }
}
