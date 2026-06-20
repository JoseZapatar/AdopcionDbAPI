
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    public int? reviewedByUserId { get; set; }

    public DateTime? reviewedAt { get; set; }

    [StringLength(1000)]
    public string? decisionNotes { get; set; }

    [ForeignKey("adopterId")]
    [InverseProperty("AdoptionRequests")]
    public virtual Adopter adopter { get; set; } = null!;

    [ForeignKey("petId")]
    [InverseProperty("AdoptionRequests")]
    public virtual Pet pet { get; set; } = null!;

    [ForeignKey("statusId")]
    [InverseProperty("AdoptionRequests")]
    public virtual RequestStatus status { get; set; } = null!;

    [ForeignKey("reviewedByUserId")]
    [InverseProperty("ReviewedAdoptionRequests")]
    public virtual User? reviewedByUser { get; set; }
}