using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdopcionDbAPI.Models;

public partial class PublisherRequest
{
    [Key]
    public int id { get; set; }

    public int userId { get; set; }

    [StringLength(30)]
    public string status { get; set; } = "Pendiente";

    public DateTime requestedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    public int? reviewedByUserId { get; set; }

    [StringLength(1000)]
    public string? decisionNotes { get; set; }

    public byte[] identificationImageData { get; set; } = null!;

    [StringLength(100)]
    public string identificationImageContentType { get; set; } = null!;

    [ForeignKey("userId")]
    [InverseProperty("PublisherRequests")]
    public virtual User user { get; set; } = null!;

    [ForeignKey("reviewedByUserId")]
    [InverseProperty("ReviewedPublisherRequests")]
    public virtual User? reviewedByUser { get; set; }
}
