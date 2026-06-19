using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Keyless]
public partial class vw_PetReviewSummary
{
    public int petId { get; set; }

    [StringLength(100)]
    public string petName { get; set; } = null!;

    public int? totalReviews { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? averageRating { get; set; }
}
