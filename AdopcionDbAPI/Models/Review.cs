using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

[Index("userId", "petId", Name = "UX_Reviews_User_Pet", IsUnique = true)]
public partial class Review
{
    [Key]
    public int id { get; set; }

    public int userId { get; set; }

    public int petId { get; set; }

    public int rating { get; set; }

    public string? comment { get; set; }

    public DateTime createdAt { get; set; }

    [ForeignKey("petId")]
    [InverseProperty("Reviews")]
    public virtual Pet pet { get; set; } = null!;

    [ForeignKey("userId")]
    [InverseProperty("Reviews")]
    public virtual User user { get; set; } = null!;
}
