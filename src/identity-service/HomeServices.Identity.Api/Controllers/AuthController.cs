using HomeServices.Identity.Application.Dtos;
using HomeServices.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Identity.Api.Controllers;

/// <summary>
/// Authentication endpoints: register, login, refresh and revoke.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user (Customer or Expert).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Succeeded = false,
                Message = "Invalid request.",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList(),
            });
        }

        var response = await _authService.RegisterAsync(request, cancellationToken);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>Login and receive a JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Succeeded = false, Message = "Invalid request." });
        }

        var response = await _authService.LoginAsync(request, cancellationToken);
        return response.Succeeded ? Ok(response) : Unauthorized(response);
    }

    /// <summary>Refresh an access token using a refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return response.Succeeded ? Ok(response) : Unauthorized(response);
    }

    /// <summary>Revoke a refresh token.</summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
