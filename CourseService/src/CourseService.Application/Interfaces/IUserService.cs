namespace CourseService.Application.Interfaces;

/// <summary>
/// User service interface for inter-service communication with UserService.
/// </summary>
public interface IUserService
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default);
}
