using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.DTOs.Auth;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        TimeProvider? timeProvider = null)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        try
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(dto.Email, ct))
            {
                return Result<AuthResponseDto>.Failure("Email already registered");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = passwordHash,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                LastLoginAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            var createdUser = await _userRepository.CreateAsync(user, ct);

            // Generate JWT token
            var token = GenerateJwtToken(createdUser);
            var expiresAt = _timeProvider.GetUtcNow().AddHours(24).UtcDateTime;

            _logger.LogInformation("User registered successfully: {Email}", dto.Email);

            var response = new AuthResponseDto(
                createdUser.Id,
                createdUser.Email,
                createdUser.FirstName,
                createdUser.LastName,
                createdUser.Role.ToString(),
                token,
                expiresAt
            );

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Registration failed. Please try again.");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(dto.Email, ct);
            if (user is null)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<AuthResponseDto>.Failure("Account is inactive");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiresAt = _timeProvider.GetUtcNow().AddHours(24).UtcDateTime;

            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

            var response = new AuthResponseDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                token,
                expiresAt
            );

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", dto.Email);
            return Result<AuthResponseDto>.Failure("Login failed. Please try again.");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "CinemaBookingAPI";
        var audience = jwtSettings["Audience"] ?? "CinemaBookingClient";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: _timeProvider.GetUtcNow().AddHours(24).UtcDateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
