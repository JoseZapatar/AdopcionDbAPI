using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Keyless]
public partial class vw_AdoptersProfile
{
    public int adopterId { get; set; }

    [StringLength(100)]
    public string adopterName { get; set; } = null!;

    [StringLength(150)]
    public string email { get; set; } = null!;

    [StringLength(30)]
    public string? phone { get; set; }

    [StringLength(255)]
    public string? address { get; set; }

    [StringLength(100)]
    public string? city { get; set; }

    [StringLength(50)]
    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }

    public int? totalRequests { get; set; }
}
