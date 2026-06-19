using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("name", Name = "UQ__Sizes__72E12F1B9D84A21E", IsUnique = true)]
public partial class Size
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string name { get; set; } = null!;

    [InverseProperty("size")]
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
