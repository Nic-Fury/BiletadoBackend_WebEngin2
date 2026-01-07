using Biletado.DTOs;

namespace Biletado.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    string GenerateJwtToken(string username, Guid userId);
}
