using Application.DTOs.Auth;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Tests.Unit.Helpers;
using Xunit;

namespace Tests.Unit.Services;

/// <summary>
/// Integration tests confirming that <see cref="AuthService"/> surfaces
/// Bulgarian-language error messages when the localizer is configured with the
/// <c>bg-BG</c> culture.  These tests exercise the service's Result.Error paths
/// without touching the database.
/// </summary>
public class AuthServiceBgTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IAuthService _authService;

    public AuthServiceBgTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc));

        // Minimal JWT section so AuthService can be constructed (it only reads config
        // on the success path; error-path tests never reach token generation).
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["SecretKey"]).Returns("test-secret-key-256bits-test-secret-key");
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSection.Object);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            LocalizerHelper.CreateBg(),
            _timeProvider);
    }

    // ─── RegisterAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_ReturnsBulgarianError()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new RegisterDto(
            Email: "existing@example.com",
            Password: "ValidPass1",
            FirstName: "Иван",
            LastName: "Петров",
            PhoneNumber: "+35988123456");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Имейлът вече е регистриран");
    }

    [Fact]
    public async Task RegisterAsync_RepositoryThrows_ReturnsBulgarianError()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        var dto = new RegisterDto(
            Email: "new@example.com",
            Password: "ValidPass1",
            FirstName: "Мария",
            LastName: "Иванова",
            PhoneNumber: "+35987654321");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Регистрацията е неуспешна. Моля, опитайте отново.");
    }

    // ─── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsBulgarianError()
    {
        // Arrange – repository returns null (user does not exist)
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var dto = new LoginDto(Email: "nobody@example.com", Password: "Password1");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Невалиден имейл или парола");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsBulgarianError()
    {
        // Arrange – user exists but supplied password doesn't match the stored hash
        var user = BuildUser(isActive: true, passwordPlaintext: "CorrectPass1");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var dto = new LoginDto(Email: user.Email, Password: "WrongPass1");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Невалиден имейл или парола");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsBulgarianError()
    {
        // Arrange – correct credentials but account is inactive
        const string password = "CorrectPass1";
        var user = BuildUser(isActive: false, passwordPlaintext: password);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var dto = new LoginDto(Email: user.Email, Password: password);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Акаунтът е неактивен");
    }

    [Fact]
    public async Task LoginAsync_RepositoryThrows_ReturnsBulgarianError()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        var dto = new LoginDto(Email: "user@example.com", Password: "Password1");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Влизането е неуспешно. Моля, опитайте отново.");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static User BuildUser(bool isActive, string passwordPlaintext) => new()
    {
        Id = Guid.NewGuid(),
        Email = "ivan@example.bg",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordPlaintext),
        FirstName = "Иван",
        LastName = "Петров",
        Role = UserRole.Customer,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        LastLoginAt = DateTime.UtcNow
    };
}
