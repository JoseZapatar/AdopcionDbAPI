using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("name", Name = "UQ__Roles__72E12F1BE92F9CD4", IsUnique = true)]
public partial class Role
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string name { get; set; } = null!;

    [InverseProperty("role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
