using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("name", Name = "UQ__PetStatu__72E12F1B1481EED0", IsUnique = true)]
public partial class PetStatus
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string name { get; set; } = null!;

    [InverseProperty("status")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
