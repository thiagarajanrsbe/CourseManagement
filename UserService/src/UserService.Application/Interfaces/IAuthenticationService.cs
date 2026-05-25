using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Authentication service interface.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthTokenDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
    Task<AuthTokenDto> LoginAsync(LoginUserDto dto, CancellationToken cancellationToken = default);
    string GenerateJwtToken(User user);
}

/// <summary>
/// User service interface.
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
