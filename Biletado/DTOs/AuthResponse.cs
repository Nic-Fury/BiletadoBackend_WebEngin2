namespace Biletado.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
