using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AdopcionDbAPI.Models;

public partial class AdoptionRequest
{
    [Key]
    public int id { get; set; }

    public int adopterId { get; set; }

    public int petId { get; set; }

    public string? message { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }

    public int statusId { get; set; }

    [ForeignKey("adopterId")]
    [InverseProperty("AdoptionRequests")]
    public virtual Adopter adopter { get; set; } = null!;

    [ForeignKey("petId")]
    [InverseProperty("AdoptionRequests")]
    public virtual Pet pet { get; set; } = null!;

    [ForeignKey("statusId")]
    [InverseProperty("AdoptionRequests")]
    public virtual RequestStatus status { get; set; } = null!;
}
