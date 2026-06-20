using System;
using System.ComponentModel.DataAnnotations;
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

    public bool isVaccinated { get; set; }

    public bool isSterilized { get; set; }

    public bool isDewormed { get; set; }

    [StringLength(1000)]
    public string? medicalNotes { get; set; }

    public DateOnly? rescuedAt { get; set; }
}