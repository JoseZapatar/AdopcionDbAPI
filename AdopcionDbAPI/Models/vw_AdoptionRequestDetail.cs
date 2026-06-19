using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Keyless]
public partial class vw_AdoptionRequestDetail
{
    public int requestId { get; set; }

    [StringLength(100)]
    public string adopterName { get; set; } = null!;

    [StringLength(150)]
    public string email { get; set; } = null!;

    [StringLength(30)]
    public string? phone { get; set; }

    [StringLength(100)]
    public string? city { get; set; }

    [StringLength(100)]
    public string petName { get; set; } = null!;

    [StringLength(50)]
    public string speciesName { get; set; } = null!;

    [StringLength(100)]
    public string? breedName { get; set; }

    [StringLength(50)]
    public string requestStatus { get; set; } = null!;

    public string? message { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }
}
