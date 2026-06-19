using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("speciesId", "name", Name = "UQ_Breeds_Species_Name", IsUnique = true)]
public partial class Breed
{
    [Key]
    public int id { get; set; }

    public int speciesId { get; set; }

    [StringLength(100)]
    public string name { get; set; } = null!;

    [InverseProperty("breed")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    [ForeignKey("speciesId")]
    [InverseProperty("Breeds")]
    public virtual Species species { get; set; } = null!;
}
