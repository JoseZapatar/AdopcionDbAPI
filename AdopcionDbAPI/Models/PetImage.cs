using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

public partial class PetImage
{
    [Key]
    public int id { get; set; }

    public int petId { get; set; }

    public byte[] imageData { get; set; } = null!;

    [StringLength(100)]
    public string imageContentType { get; set; } = null!;

    public bool isPrimary { get; set; }

    public DateTime createdAt { get; set; }

    [ForeignKey("petId")]
    [InverseProperty("PetImage")]
    public virtual Pet pet { get; set; } = null!;
}
