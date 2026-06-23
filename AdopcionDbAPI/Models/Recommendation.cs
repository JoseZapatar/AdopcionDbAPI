using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdopcionDbAPI.Models;

public partial class Recommendation
{
    [Key]
    public int id { get; set; }

    public int? userId { get; set; }

    [StringLength(100)]
    public string? name { get; set; }

    [StringLength(150)]
    public string? email { get; set; }

    public string message { get; set; } = null!;

    [StringLength(30)]
    public string status { get; set; } = "Pendiente";

    [StringLength(1000)]
    public string? adminNotes { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? user { get; set; }
}
