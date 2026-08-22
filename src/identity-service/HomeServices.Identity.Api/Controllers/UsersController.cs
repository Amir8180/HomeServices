using HomeServices.Identity.Application.Dtos;
using HomeServices.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Identity.Api.Controllers;

/// <summary>
/// User management endpoints consumed by the MVC application and admin panel.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require a valid JWT
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get a user by id. Used by other services (server-to-server, no JWT attached)
    /// to resolve display info — returns the minimal public UserDto only.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HomeServices.Shared.Dtos.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeServices.Shared.Dtos.UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>Get a user by email (public lookup, minimal UserDto).</summary>
    [HttpGet("by-email/{email}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HomeServices.Shared.Dtos.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeServices.Shared.Dtos.UserDto>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByEmailAsync(email, cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>List all users (admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<HomeServices.Shared.Dtos.UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HomeServices.Shared.Dtos.UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>Update the current user's profile.</summary>
    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var ok = await _userService.UpdateProfileAsync(id, request, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Change the current user's password.</summary>
    [HttpPost("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var ok = await _userService.ChangePasswordAsync(id, request, cancellationToken);
        return ok ? NoContent() : BadRequest("Password change failed.");
    }

    /// <summary>Activate/deactivate a user (admin only).</summary>
    [HttpPost("{id:guid}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _userService.ToggleUserStatusAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
