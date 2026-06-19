using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("userId", Name = "UQ__Adopters__CB9A1CFED694980F", IsUnique = true)]
public partial class Adopter
{
    [Key]
    public int id { get; set; }

    public int userId { get; set; }

    [StringLength(30)]
    public string? phone { get; set; }

    [StringLength(255)]
    public string? address { get; set; }

    [StringLength(100)]
    public string? city { get; set; }

    [StringLength(50)]
    public string? housingType { get; set; }

    public bool hasOtherPets { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }

    [InverseProperty("adopter")]
    public virtual ICollection<AdoptionRequest> AdoptionRequests { get; set; } = new List<AdoptionRequest>();

    [ForeignKey("userId")]
    [InverseProperty("Adopter")]
    public virtual User user { get; set; } = null!;
}
