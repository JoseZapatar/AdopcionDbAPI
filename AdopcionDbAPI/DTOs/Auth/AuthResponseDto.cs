namespace AdopcionDbAPI.DTOs.Auth;

public class AuthResponseDto
{
    public string token { get; set; } = null!;

    public DateTime expiresAt { get; set; }

    public AuthUserDto user { get; set; } = null!;
}