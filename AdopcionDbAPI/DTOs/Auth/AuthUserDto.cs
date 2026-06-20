namespace AdopcionDbAPI.DTOs.Auth;

public class AuthUserDto
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string email { get; set; } = null!;

    public int roleId { get; set; }

    public string roleName { get; set; } = null!;
}