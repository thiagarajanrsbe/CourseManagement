using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using Common.Exceptions;

namespace UserService.Infrastructure.Services;

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public AuthenticationService(
        IUserRepository userRepository,
        string jwtSecret,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpirationMinutes = 60)
    {
        _userRepository = userRepository;
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
        _jwtExpirationMinutes = jwtExpirationMinutes;
    }

    public async Task<AuthTokenDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        // Validation
        if (await _userRepository.UsernameExistsAsync(dto.Username, cancellationToken))
        {
            throw new DuplicateException($"Username '{dto.Username}' is already taken.");
        } 
        if (await _userRepository.EmailExistsAsync(dto.Email, cancellationToken))
        {
            throw new DuplicateException($"Email '{dto.Email}' is already registered.");
        }

        // Hash password
        var passwordHash = HashPassword(dto.Password);

        // Create user
        var user = User.Create(dto.Username, dto.Email, passwordHash, dto.Role);

        // Save to repository
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Generate token
        var token = GenerateJwtToken(user);

        return new AuthTokenDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            AccessToken = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
        };
    }

    public async Task<AuthTokenDto> LoginAsync(LoginUserDto dto, CancellationToken cancellationToken = default)
    {
        // Find user by username
        var user = await _userRepository.GetByUsernameAsync(dto.Username, cancellationToken);

        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new Common.Exceptions.UnauthorizedAccessException("User account is inactive.");
        }

        // Generate token
        var token = GenerateJwtToken(user);

        return new AuthTokenDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            AccessToken = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
        };
    }

    public string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username),
            new(System.Security.Claims.ClaimTypes.Email, user.Email),
            new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        using var sha256 = SHA256.Create();
        var hashOfInput = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hashBytes = Convert.FromBase64String(hash);
        return hashOfInput.SequenceEqual(hashBytes);
    }
}
