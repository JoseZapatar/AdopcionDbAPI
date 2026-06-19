using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("name", Name = "UQ__Species__72E12F1BE38FA7EB", IsUnique = true)]
public partial class Species
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string name { get; set; } = null!;

    [InverseProperty("species")]
    public virtual ICollection<Breed> Breeds { get; set; } = new List<Breed>();

    [InverseProperty("species")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
