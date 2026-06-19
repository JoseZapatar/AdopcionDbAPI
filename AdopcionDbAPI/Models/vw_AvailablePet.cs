using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Keyless]
public partial class vw_AvailablePet
{
    public int id { get; set; }

    [StringLength(100)]
    public string petName { get; set; } = null!;

    [StringLength(50)]
    public string speciesName { get; set; } = null!;

    [StringLength(100)]
    public string? breedName { get; set; }

    [StringLength(50)]
    public string sizeName { get; set; } = null!;

    [StringLength(50)]
    public string statusName { get; set; } = null!;

    public int? ageCalculated { get; set; }

    [StringLength(20)]
    public string? gender { get; set; }

    public string? description { get; set; }

    public DateTime createdAt { get; set; }
}
