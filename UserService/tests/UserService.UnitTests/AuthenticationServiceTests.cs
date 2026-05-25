using System.Security.Cryptography;
using System.Text;
using Moq;
using Xunit;
using UserService.Infrastructure.Services;
using UserService.Domain.Interfaces;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using Common.Exceptions;
using System.Threading.Tasks;
using System;

namespace UserService.UnitTests;

public class AuthenticationServiceTests
{
    private const string JwtSecret = "test_secret_key_for_unit_test_to_run";
    private const string JwtIssuer = "test_issuer";
    private const string JwtAudience = "test_audience";

    [Fact]
    public async Task RegisterAsync_Success()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var svc = new AuthenticationService(repoMock.Object, JwtSecret, JwtIssuer, JwtAudience, 60);

        var dto = new RegisterUserDto { Username = "testuser", Email = "testuser@test", Password = "P@ssw0rd", Role = UserRole.Student };

        var result = await svc.RegisterAsync(dto);

        Assert.Equal(dto.Username, result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        repoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == dto.Username), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_Throws()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.UsernameExistsAsync("testuser2", default)).ReturnsAsync(true);

        var svc = new AuthenticationService(repoMock.Object, JwtSecret, JwtIssuer, JwtAudience, 60);

        var dto = new RegisterUserDto { Username = "testuser2", Email = "testuser2@test", Password = "x", Role = UserRole.Student };

        await Assert.ThrowsAsync<DuplicateException>(() => svc.RegisterAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_Success()
    {
        var password = "Secret123";
        var hash = ComputeSha256Base64(password);

        var user = User.Create("testuser3", "testuser3@test", hash, UserRole.Student);

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByUsernameAsync("testuser3", default)).ReturnsAsync(user);

        var svc = new AuthenticationService(repoMock.Object, JwtSecret, JwtIssuer, JwtAudience, 60);

        var dto = new LoginUserDto { Username = "testuser3", Password = password };

        var result = await svc.LoginAsync(dto);

        Assert.Equal(user.Username, result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        var password = "Secret123";
        var hash = ComputeSha256Base64(password);

        var user = User.Create("testuser4", "testuser4@test", hash, UserRole.Student);

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByUsernameAsync("testuser4", default)).ReturnsAsync(user);

        var svc = new AuthenticationService(repoMock.Object, JwtSecret, JwtIssuer, JwtAudience, 60);

        var dto = new LoginUserDto { Username = "testuser4", Password = "wrong" };

        await Assert.ThrowsAsync<Common.Exceptions.UnauthorizedAccessException>(() => svc.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_Throws()
    {
        var password = "Secret123";
        var hash = ComputeSha256Base64(password);

        var user = User.Create("testuser5", "testuser5@test", hash, UserRole.Student);
        user.IsActive = false;

        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByUsernameAsync("testuser5", default)).ReturnsAsync(user);

        var svc = new AuthenticationService(repoMock.Object, JwtSecret, JwtIssuer, JwtAudience, 60);

        var dto = new LoginUserDto { Username = "testuser5", Password = password };

        await Assert.ThrowsAsync<Common.Exceptions.UnauthorizedAccessException>(() => svc.LoginAsync(dto));
    }

    private static string ComputeSha256Base64(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
