namespace AdopcionDbAPI.DTOs.Reviews;

public class ReviewDto
{
    public int id { get; set; }

    public int userId { get; set; }

    public string userName { get; set; } = null!;

    public int petId { get; set; }

    public string petName { get; set; } = null!;

    public int rating { get; set; }

    public string? comment { get; set; }

    public DateTime createdAt { get; set; }
}