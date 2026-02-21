using Application.DTOs.Auth;
using Application.Resources;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Unit.Builders;

namespace Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _localizerMock = new Mock<IStringLocalizer<SharedResource>>();
        _timeProviderMock = new Mock<TimeProvider>();

        // Default localizer returns the key as the value
        _localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        // Setup JWT configuration
        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(x => x["SecretKey"]).Returns("test-secret-key-with-at-least-32-characters");
        jwtSectionMock.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSectionMock.Setup(x => x["Audience"]).Returns("TestAudience");
        _configurationMock.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSectionMock.Object);

        _timeProviderMock.Setup(x => x.GetUtcNow())
            .Returns(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _localizerMock.Object,
            _timeProviderMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidDto_ReturnsSuccessWithToken()
    {
        // Arrange
        var dto = new RegisterDto(
            "newuser@example.com",
            "Password123",
            "John",
            "Doe",
            "+1234567890"
        );

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(dto.Email);
        result.Value.Token.Should().NotBeEmpty();
        result.Value.Role.Should().Be("Customer");

        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var dto = new RegisterDto(
            "existing@example.com",
            "Password123",
            "John",
            "Doe",
            "+1234567890"
        );

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already registered");

        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .Build();

        var dto = new LoginDto("test@example.com", "Password123");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(user.Email);
        result.Value.Token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var dto = new LoginDto("nonexistent@example.com", "Password123");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .Build();

        var dto = new LoginDto("test@example.com", "WrongPassword");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .AsInactive()
            .Build();

        var dto = new LoginDto("test@example.com", "Password123");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is inactive");
    }
}
