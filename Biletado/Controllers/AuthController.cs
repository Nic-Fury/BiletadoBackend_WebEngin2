using Biletado.DTOs;
using Biletado.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Registration request received for username: {Username}", request.Username);

        var response = await _authService.RegisterAsync(request, ct);

        if (response == null)
        {
            _logger.LogWarning("Registration failed for username: {Username}", request.Username);
            return BadRequest(new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "registration_failed",
                        Message = "Registration failed. Username must be at least 3 characters, password at least 6 characters, and username must not already exist."
                    }
                }
            });
        }

        _logger.LogInformation("User registered successfully: {Username}", response.Username);
        return CreatedAtAction(nameof(Register), new { username = response.Username }, response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Login request received for username: {Username}", request.Username);

        var response = await _authService.LoginAsync(request, ct);

        if (response == null)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "authentication_failed",
                        Message = "Invalid username or password."
                    }
                }
            });
        }

        _logger.LogInformation("User logged in successfully: {Username}", response.Username);
        return Ok(response);
    }
}
