using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.CinemaHalls;
using Application.DTOs.Movies;
using Application.DTOs.Showtimes;
using Backend.Models;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Integration.Helpers;

public static class TestDataHelper
{
    public static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string password = "Test123!")
    {
        // Register
        var registerDto = new RegisterDto(
            email,
            password,
            "Test",
            "User",
            "+1234567890"
        );

        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Login
        var loginDto = new LoginDto(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

        return loginResult?.Data?.Token ?? throw new InvalidOperationException("Failed to get auth token");
    }

    public static async Task<string> CreateAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Create admin user directly in database
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"admin-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "+1234567890",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Generate JWT token
        var jwtSecret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret not configured");
        var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "CinemaBookingAPI";
        var jwtAudience = configuration["JwtSettings:Audience"] ?? "CinemaBookingClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, adminUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, adminUser.Email),
            new Claim(ClaimTypes.Role, adminUser.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static async Task<string> RegisterAndLoginAdminAsync(IServiceProvider serviceProvider)
    {
        return await CreateAdminUserAsync(serviceProvider);
    }

    public static async Task<Guid> CreateMovieAsync(HttpClient client, string token, string title = "Test Movie")
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var movieDto = new CreateMovieDto(
            title,
            "A test movie",
            "Action",
            120,
            "PG-13",
            "https://example.com/poster.jpg",
            DateOnly.FromDateTime(DateTime.Today.AddMonths(-1))
        );

        var response = await client.PostAsJsonAsync("/api/movies", movieDto);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MovieDto>>();
        return result?.Data?.Id ?? throw new InvalidOperationException("Failed to create movie");
    }

    public static async Task<Guid> CreateHallAsync(HttpClient client, string token, string name = "Test Hall")
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Simple 5x5 hall layout
        var seats = new List<SeatDefinition>();
        for (int row = 0; row < 5; row++)
        {
            for (int col = 1; col <= 5; col++)
            {
                char rowLetter = (char)('A' + row);
                seats.Add(new SeatDefinition
                {
                    SeatNumber = $"{rowLetter}{col}",
                    Row = row,
                    Column = col - 1,
                    SeatType = "Regular",
                    PriceMultiplier = 1.0m,
                    IsAvailable = true
                });
            }
        }

        var seatLayout = new SeatLayout
        {
            Rows = 5,
            SeatsPerRow = 5,
            Seats = seats
        };

        var hallDto = new CreateCinemaHallDto(name, seatLayout);

        var response = await client.PostAsJsonAsync("/api/halls", hallDto);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CinemaHallDto>>();
        return result?.Data?.Id ?? throw new InvalidOperationException("Failed to create hall");
    }

    public static async Task<Guid> CreateShowtimeAsync(
        HttpClient client,
        string token,
        Guid movieId,
        Guid hallId,
        DateTime? startTime = null)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var showtimeDto = new CreateShowtimeDto(
            movieId,
            hallId,
            startTime ?? DateTime.UtcNow.AddHours(2),
            10.00m
        );

        var response = await client.PostAsJsonAsync("/api/showtimes", showtimeDto);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShowtimeDto>>();
        return result?.Data?.Id ?? throw new InvalidOperationException("Failed to create showtime");
    }

    public static async Task<(Guid showtimeId, string token)> SetupBookingScenarioAsync(
        HttpClient client,
        IServiceProvider serviceProvider)
    {
        // Create admin user
        var token = await RegisterAndLoginAdminAsync(serviceProvider);

        // Create movie
        var movieId = await CreateMovieAsync(client, token);

        // Create hall
        var hallId = await CreateHallAsync(client, token);

        // Create showtime
        var showtimeId = await CreateShowtimeAsync(client, token, movieId, hallId);

        return (showtimeId, token);
    }

    public static async Task<(Guid showtimeId, string token1, string token2)> SetupConcurrentBookingScenarioAsync(
        HttpClient client,
        IServiceProvider serviceProvider)
    {
        // Create admin user to set up movie/hall/showtime
        var adminToken = await RegisterAndLoginAdminAsync(serviceProvider);

        // Create movie
        var movieId = await CreateMovieAsync(client, adminToken);

        // Create hall
        var hallId = await CreateHallAsync(client, adminToken);

        // Create showtime
        var showtimeId = await CreateShowtimeAsync(client, adminToken, movieId, hallId);

        // Create two regular users for booking
        var token1 = await RegisterAndLoginAsync(client, $"user1-{Guid.NewGuid()}@test.com");
        var token2 = await RegisterAndLoginAsync(client, $"user2-{Guid.NewGuid()}@test.com");

        return (showtimeId, token1, token2);
    }
}
