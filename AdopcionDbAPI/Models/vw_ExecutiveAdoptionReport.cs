using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Keyless]
public partial class vw_ExecutiveAdoptionReport
{
    [StringLength(50)]
    public string speciesName { get; set; } = null!;

    [StringLength(50)]
    public string petStatus { get; set; } = null!;

    public int? totalPets { get; set; }

    public int? totalRequests { get; set; }

    public int? approvedRequests { get; set; }

    public int? pendingRequests { get; set; }
}
