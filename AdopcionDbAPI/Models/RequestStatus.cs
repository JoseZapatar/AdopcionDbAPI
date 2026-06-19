using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("name", Name = "UQ__RequestS__72E12F1B98AE630A", IsUnique = true)]
public partial class RequestStatus
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string name { get; set; } = null!;

    [InverseProperty("status")]
    public virtual ICollection<AdoptionRequest> AdoptionRequests { get; set; } = new List<AdoptionRequest>();
}
