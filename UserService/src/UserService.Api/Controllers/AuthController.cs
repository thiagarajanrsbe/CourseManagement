using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using Common.Exceptions;

namespace UserService.Api.Controllers;

/// <summary>
/// Authentication controller for user registration and login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IValidator<RegisterUserDto> _registerValidator;
    private readonly IValidator<LoginUserDto> _loginValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        IValidator<RegisterUserDto> registerValidator,
        IValidator<LoginUserDto> loginValidator,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Register(
        [FromBody] RegisterUserDto dto,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Registration validation failed for username: {Username}", dto.Username);
            return BadRequest(ApiResponse<AuthTokenDto>.ErrorResponse("Validation failed.", errors));
        }

        _logger.LogInformation("User registration initiated for username: {Username}", dto.Username);

        var result = await _authenticationService.RegisterAsync(dto, cancellationToken);
        _logger.LogInformation("User registered successfully: {UserId}", result.UserId);

        return Ok(ApiResponse<AuthTokenDto>.SuccessResponse(result, "User registered successfully."));
    }

    /// <summary>
    /// Login a user.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Login(
        [FromBody] LoginUserDto dto,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Login validation failed for username: {Username}", dto.Username);
            return BadRequest(ApiResponse<AuthTokenDto>.ErrorResponse("Validation failed.", errors));
        }

        _logger.LogInformation("User login initiated for username: {Username}", dto.Username);

        var result = await _authenticationService.LoginAsync(dto, cancellationToken);
        _logger.LogInformation("User logged in successfully: {UserId}", result.UserId);

        return Ok(ApiResponse<AuthTokenDto>.SuccessResponse(result, "Login successful."));
    }
}
