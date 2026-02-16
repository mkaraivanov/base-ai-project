using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Backend.Models;
using FluentAssertions;

namespace Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidDto_ReturnsOkWithToken()
    {
        // Arrange
        var dto = new RegisterDto(
            $"test{Guid.NewGuid()}@example.com",
            "Password123",
            "John",
            "Doe",
            "+1234567890"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeEmpty();
        result.Data.Email.Should().Be(dto.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate{Guid.NewGuid()}@example.com";
        var dto1 = new RegisterDto(email, "Password123", "John", "Doe", "+1234567890");
        var dto2 = new RegisterDto(email, "Password123", "Jane", "Smith", "+9876543210");

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", dto1);
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange - First register
        var registerDto = new RegisterDto(
            $"login{Guid.NewGuid()}@example.com",
            "Password123",
            "John",
            "Doe",
            "+1234567890"
        );
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto(registerDto.Email, registerDto.Password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto("nonexistent@example.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
