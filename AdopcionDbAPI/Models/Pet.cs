using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

public partial class Pet
{
    [Key]
    public int id { get; set; }

    public int speciesId { get; set; }

    public int sizeId { get; set; }

    public int statusId { get; set; }

    public int? publisherUserId { get; set; }

    [StringLength(100)]
    public string name { get; set; } = null!;

    [StringLength(20)]
    public string? gender { get; set; }

    public string? description { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }

    public DateOnly? birthDate { get; set; }

    public int? breedId { get; set; }

    public bool isVaccinated { get; set; }

    public bool isSterilized { get; set; }

    public bool isDewormed { get; set; }

    [StringLength(1000)]
    public string? medicalNotes { get; set; }

    public DateOnly? rescuedAt { get; set; }

    [InverseProperty("pet")]
    public virtual ICollection<AdoptionRequest> AdoptionRequests { get; set; } = new List<AdoptionRequest>();

    [InverseProperty("pet")]
    public virtual ICollection<PetImage> PetImages { get; set; } = new List<PetImage>();

    [InverseProperty("pet")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("breedId")]
    [InverseProperty("Pets")]
    public virtual Breed? breed { get; set; }

    [ForeignKey("sizeId")]
    [InverseProperty("Pets")]
    public virtual Size size { get; set; } = null!;

    [ForeignKey("speciesId")]
    [InverseProperty("Pets")]
    public virtual Species species { get; set; } = null!;

    [ForeignKey("statusId")]
    [InverseProperty("Pets")]
    public virtual PetStatus status { get; set; } = null!;

    [ForeignKey("publisherUserId")]
    public virtual User? publisherUser { get; set; }
}
