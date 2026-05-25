using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using Common.Exceptions;

namespace UserService.Api.Controllers;

/// <summary>
/// Users controller for user operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);

        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            return NotFound(ApiResponse<UserDto>.ErrorResponse($"User with ID {id} not found."));
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User retrieved successfully."));
    }

    /// <summary>
    /// Get current user profile.
    /// </summary>
    [HttpGet("profile/me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUserProfile(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID in token claims.");
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Invalid user token."));
        }

        _logger.LogInformation("Fetching current user profile: {UserId}", userId);

        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Current user not found: {UserId}", userId);
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("User not found."));
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Profile retrieved successfully."));
    }
}
