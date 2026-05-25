namespace UserService.Domain.Entities;

/// <summary>
/// User role enumeration for role-based authorization.
/// </summary>
public enum UserRole
{
    Instructor,
    Student
}

/// <summary>
/// User domain entity.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    private User() { }

    /// <summary>
    /// Factory method to create a new User.
    /// </summary>
    public static User Create(string username, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates user information.
    /// </summary>
    public void Update(string username, string email)
    {
        Username = username;
        Email = email;
    }
}
