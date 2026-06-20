using System.ComponentModel.DataAnnotations;

namespace AdopcionDbAPI.DTOs.Reviews;

public class UpdateReviewDto
{
    [Range(1, 5)]
    public int rating { get; set; }

    public string? comment { get; set; }
}