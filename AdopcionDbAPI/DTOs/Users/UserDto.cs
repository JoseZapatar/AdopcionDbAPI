namespace AdopcionDbAPI.DTOs.Users;

public class UserDto
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string email { get; set; } = null!;

    public int roleId { get; set; }

    public string roleName { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public bool hasAdopterProfile { get; set; }
}