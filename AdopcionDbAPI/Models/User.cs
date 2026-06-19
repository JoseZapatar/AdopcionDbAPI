using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("email", Name = "UQ__Users__AB6E616449033E67", IsUnique = true)]
public partial class User
{
    [Key]
    public int id { get; set; }

    public int roleId { get; set; }

    [StringLength(100)]
    public string name { get; set; } = null!;

    [StringLength(150)]
    public string email { get; set; } = null!;

    [StringLength(255)]
    public string passwordHash { get; set; } = null!;

    public DateTime createdAt { get; set; }

    [InverseProperty("user")]
    public virtual Adopter? Adopter { get; set; }

    [InverseProperty("user")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("roleId")]
    [InverseProperty("Users")]
    public virtual Role role { get; set; } = null!;
}
