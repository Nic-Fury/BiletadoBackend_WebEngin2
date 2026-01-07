using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Biletado.Contexts;
using Biletado.Domain;
using Biletado.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;

namespace Biletado.Services;

public class AuthService : IAuthService
{
    private readonly ReservationsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ReservationsDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to register user: {Username}", request.Username);

        // Validate input
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            _logger.LogWarning("Registration failed: Username too short");
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            _logger.LogWarning("Registration failed: Password too short");
            return null;
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Username already exists: {Username}", request.Username);
            return null;
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User registered successfully: UserId={UserId}, Username={Username}",
            user.Id, user.Username);

        var token = GenerateJwtToken(user.Username, user.Id);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to login user: {Username}", request.Username);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found: {Username}", request.Username);
            return null;
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user: {Username}", request.Username);
            return null;
        }

        _logger.LogInformation("User logged in successfully: UserId={UserId}, Username={Username}",
            user.Id, user.Username);

        var token = GenerateJwtToken(user.Username, user.Id);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            ExpiresAt = expiresAt
        };
    }

    public string GenerateJwtToken(string username, Guid userId)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "BiletadoDefaultSecretKeyForDevelopmentOnly123!";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BiletadoAPI";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BiletadoUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
