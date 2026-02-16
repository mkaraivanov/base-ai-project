# Phase 1: Foundation & Authentication

**Duration:** Week 1
**Status:** 🟡 In Progress

## Overview

This phase establishes the foundational infrastructure for the cinema ticket booking system. By the end of this phase, you will have a clean architecture project structure, database connectivity with SQL Server, and a fully functional JWT-based authentication system.

## Objectives

✅ Set up clean architecture project structure (Domain, Application, Infrastructure, API)
✅ Configure required NuGet packages (EF Core, JWT, FluentValidation, Serilog)
✅ Implement SQL Server database with Entity Framework Core
✅ Build User entity with password hashing and role-based access
✅ Create authentication endpoints (register, login)
✅ Set up xUnit test projects with TestContainers
✅ Achieve 80%+ test coverage on authentication flow

---

## Step 1: Project Structure Setup

### 1.1 Create Project Structure

Create the following project structure using the .NET CLI:

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project

# Create class libraries
dotnet new classlib -n Domain -o Backend/Domain
dotnet new classlib -n Application -o Backend/Application
dotnet new classlib -n Infrastructure -o Backend/Infrastructure

# Create test projects
dotnet new xunit -n Tests.Unit -o Tests/Tests.Unit
dotnet new xunit -n Tests.Integration -o Tests/Tests.Integration

# Add projects to solution
dotnet sln base-ai-project.sln add Backend/Domain/Domain.csproj
dotnet sln base-ai-project.sln add Backend/Application/Application.csproj
dotnet sln base-ai-project.sln add Backend/Infrastructure/Infrastructure.csproj
dotnet sln base-ai-project.sln add Tests/Tests.Unit/Tests.Unit.csproj
dotnet sln base-ai-project.sln add Tests/Tests.Integration/Tests.Integration.csproj

# Add project references
cd Backend/Application
dotnet add reference ../Domain/Domain.csproj

cd ../Infrastructure
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Application/Application.csproj

cd ../Backend
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Application/Application.csproj
dotnet add reference ../Infrastructure/Infrastructure.csproj

cd ../../Tests/Tests.Unit
dotnet add reference ../../Backend/Domain/Domain.csproj
dotnet add reference ../../Backend/Application/Application.csproj

cd ../Tests.Integration
dotnet add reference ../../Backend/Backend.csproj
dotnet add reference ../../Backend/Infrastructure/Infrastructure.csproj
```

**Expected Directory Structure:**
```
base-ai-project/
├── Backend/
│   ├── Backend.csproj (API - already exists)
│   ├── Program.cs
│   ├── Domain/
│   │   ├── Domain.csproj
│   │   ├── Entities/
│   │   └── Common/
│   ├── Application/
│   │   ├── Application.csproj
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Helpers/
│   └── Infrastructure/
│       ├── Infrastructure.csproj
│       ├── Data/
│       ├── Repositories/
│       ├── UnitOfWork/
│       └── BackgroundServices/
├── Tests/
│   ├── Tests.Unit/
│   │   └── Tests.Unit.csproj
│   └── Tests.Integration/
│       └── Tests.Integration.csproj
└── docs/
```

### 1.2 Enable Required Features

Update `.csproj` files with necessary configurations:

**Domain/Domain.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Application/Application.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>
</Project>
```

---

## Step 2: Install Required NuGet Packages

### 2.1 Backend Packages

Install packages in the Backend (API) project:

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend

# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.1
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.1
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.1

# Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.1
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.2.1

# Validation
dotnet add package FluentValidation.AspNetCore --version 11.3.0

# Logging
dotnet add package Serilog.AspNetCore --version 8.0.3
dotnet add package Serilog.Sinks.Console --version 6.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0
```

### 2.2 Infrastructure Packages

```bash
cd ../Infrastructure

dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.1
dotnet add package Microsoft.Extensions.Configuration.Abstractions --version 9.0.0
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 9.0.0
```

### 2.3 Application Packages

```bash
cd ../Application

dotnet add package FluentValidation --version 11.11.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.0
```

### 2.4 Test Packages

```bash
cd ../../Tests/Tests.Unit

dotnet add package xunit --version 2.9.3
dotnet add package xunit.runner.visualstudio --version 3.0.0
dotnet add package Moq --version 4.20.73
dotnet add package FluentAssertions --version 7.0.1
dotnet add package Microsoft.NET.Test.Sdk --version 17.13.1

cd ../Tests.Integration

dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.1
dotnet add package Testcontainers.MsSql --version 4.2.0
dotnet add package xunit --version 2.9.3
dotnet add package xunit.runner.visualstudio --version 3.0.0
dotnet add package FluentAssertions --version 7.0.1
dotnet add package Microsoft.NET.Test.Sdk --version 17.13.1
```

---

## Step 3: Implement Domain Layer

### 3.1 Create Result Pattern

**File:** `Backend/Domain/Common/Result.cs`

```csharp
namespace Domain.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    protected Result(bool isSuccess, string? error = null, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, errors: errors);
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    private Result(bool isSuccess, T? value = default, string? error = null, List<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string error) => new(false, default, error);
    public static new Result<T> Failure(List<string> errors) => new(false, default, errors: errors);
}
```

### 3.2 Create User Entity

**File:** `Backend/Domain/Entities/User.cs`

```csharp
namespace Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.Customer;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime LastLoginAt { get; init; }
}

public enum UserRole
{
    Customer = 0,
    Staff = 1,
    Admin = 2
}
```

---

## Step 4: Implement Application Layer

### 4.1 Create DTOs

**File:** `Backend/Application/DTOs/Auth/RegisterDto.cs`

```csharp
namespace Application.DTOs.Auth;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber
);
```

**File:** `Backend/Application/DTOs/Auth/LoginDto.cs`

```csharp
namespace Application.DTOs.Auth;

public record LoginDto(
    string Email,
    string Password
);
```

**File:** `Backend/Application/DTOs/Auth/AuthResponseDto.cs`

```csharp
namespace Application.DTOs.Auth;

public record AuthResponseDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Token,
    DateTime ExpiresAt
);
```

### 4.2 Create FluentValidation Validators

**File:** `Backend/Application/Validators/RegisterDtoValidator.cs`

```csharp
using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
    }
}
```

**File:** `Backend/Application/Validators/LoginDtoValidator.cs`

```csharp
using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
```

### 4.3 Create Auth Service Interface

**File:** `Backend/Application/Services/IAuthService.cs`

```csharp
using Application.DTOs.Auth;
using Domain.Common;

namespace Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
```

---

## Step 5: Implement Infrastructure Layer

### 5.1 Create DbContext

**File:** `Backend/Infrastructure/Data/CinemaDbContext.cs`

```csharp
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class CinemaDbContext : DbContext
{
    public CinemaDbContext(DbContextOptions<CinemaDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Role)
                .HasConversion<string>();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });
    }
}
```

### 5.2 Create User Repository

**File:** `Backend/Infrastructure/Repositories/IUserRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/UserRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CinemaDbContext _context;

    public UserRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, ct);
    }
}
```

### 5.3 Create Auth Service Implementation

**File:** `Backend/Infrastructure/Services/AuthService.cs`

```csharp
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
                CreatedAt = _timeProvider.GetUtcNow().DateTime,
                LastLoginAt = _timeProvider.GetUtcNow().DateTime
            };

            var createdUser = await _userRepository.CreateAsync(user, ct);

            // Generate JWT token
            var token = GenerateJwtToken(createdUser);
            var expiresAt = _timeProvider.GetUtcNow().AddHours(24).DateTime;

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
            var expiresAt = _timeProvider.GetUtcNow().AddHours(24).DateTime;

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
            expires: _timeProvider.GetUtcNow().AddHours(24).DateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Add BCrypt.Net-Next package:**
```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend/Infrastructure
dotnet add package BCrypt.Net-Next --version 4.0.3
```

---

## Step 6: Configure API Layer

### 6.1 Update appsettings.json

**File:** `Backend/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CinemaBookingDb;Trusted_Connection=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long-for-production",
    "Issuer": "CinemaBookingAPI",
    "Audience": "CinemaBookingClient"
  }
}
```

**File:** `Backend/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CinemaBookingDb_Dev;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### 6.2 Create API Response Model

**File:** `Backend/Models/ApiResponse.cs`

```csharp
namespace Backend.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    List<string>? Errors = null
);
```

### 6.3 Create Auth Endpoints

**File:** `Backend/Endpoints/AuthEndpoints.cs`

```csharp
using Application.DTOs.Auth;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new customer account");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Login user")
            .WithDescription("Authenticates user and returns JWT token");

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto dto,
        IAuthService authService,
        IValidator<RegisterDto> validator,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, "Validation failed", errors));
        }

        // Register user
        var result = await authService.RegisterAsync(dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<AuthResponseDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, result.Error));
    }

    private static async Task<IResult> LoginAsync(
        LoginDto dto,
        IAuthService authService,
        IValidator<LoginDto> validator,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, "Validation failed", errors));
        }

        // Login user
        var result = await authService.LoginAsync(dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<AuthResponseDto>(true, result.Value, null))
            : Results.Unauthorized();
    }
}
```

### 6.4 Update Program.cs

**File:** `Backend/Program.cs`

```csharp
using System.Text;
using Application.Services;
using Application.Validators;
using Backend.Endpoints;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/cinema-booking-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton(TimeProvider.System);

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

// Map endpoints
app.MapGroup("/api/auth")
    .MapAuthEndpoints()
    .WithTags("Authentication");

app.Run();

// Make Program accessible to tests
public partial class Program { }
```

---

## Step 7: Create Database Migration

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend

# Add initial migration
dotnet ef migrations add InitialCreate --project ../Infrastructure --startup-project .

# Update database
dotnet ef database update --project ../Infrastructure --startup-project .
```

---

## Step 8: Testing Setup

### 8.1 Create Test Data Builders

**File:** `Tests/Tests.Unit/Builders/UserBuilder.cs`

```csharp
using Domain.Entities;

namespace Tests.Unit.Builders;

public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = "test@example.com";
    private string _passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _phoneNumber = "+1234567890";
    private UserRole _role = UserRole.Customer;
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _lastLoginAt = DateTime.UtcNow;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _role = UserRole.Admin;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Email = _email,
            PasswordHash = _passwordHash,
            FirstName = _firstName,
            LastName = _lastName,
            PhoneNumber = _phoneNumber,
            Role = _role,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            LastLoginAt = _lastLoginAt
        };
    }
}
```

### 8.2 Create Unit Tests for AuthService

**File:** `Tests/Tests.Unit/Services/AuthServiceTests.cs`

```csharp
using Application.DTOs.Auth;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Unit.Builders;

namespace Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _timeProviderMock = new Mock<TimeProvider>();

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
```

### 8.3 Create Integration Tests

**File:** `Tests/Tests.Integration/CustomWebApplicationFactory.cs`

```csharp
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CinemaDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with TestContainers connection string
            services.AddDbContext<CinemaDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            // Build service provider and create database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<CinemaDbContext>();

            db.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
```

**File:** `Tests/Tests.Integration/AuthEndpointsTests.cs`

```csharp
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
```

---

## Step 9: Run Tests

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project

# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

---

## Step 10: Verify Implementation

### Verification Checklist

- [✅] Project structure created with clean architecture layers
- [✅] NuGet packages installed (EF Core, JWT, FluentValidation, Serilog, BCrypt)
- [✅] User entity created with role-based access
- [✅] DbContext configured with proper entity configuration
- [✅] User repository implemented
- [✅] AuthService implemented with password hashing
- [✅] JWT token generation works correctly
- [✅] Auth endpoints created (register, login)
- [✅] FluentValidation validators created
- [✅] Database migration created and applied
- [✅] Unit tests written with 80%+ coverage
- [✅] Integration tests with TestContainers
- [✅] Tests pass successfully

### Manual Testing

**Test Registration:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890"
  }'
```

**Test Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123"
  }'
```

---

## Common Issues & Solutions

### Issue 1: SQL Server LocalDB not installed
**Solution:** Install SQL Server LocalDB or use Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

Then update connection string:
```json
"DefaultConnection": "Server=localhost,1433;Database=CinemaBookingDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
```

### Issue 2: JWT SecretKey too short
**Error:** `IDX10720: Unable to create KeyedHashAlgorithm...`

**Solution:** Ensure SecretKey is at least 32 characters long in appsettings.json.

### Issue 3: TestContainers requires Docker
**Error:** `Docker is not running`

**Solution:** Install and start Docker Desktop before running integration tests.

---

## Next Steps

✅ **Phase 1 Complete!**

Proceed to Phase 2: Content Management (Movies, Halls, Showtimes)

**Phase 2 Preview:**
- Movie CRUD with admin endpoints
- Cinema hall management with JSON seat layouts
- Showtime scheduling with business rules
- Seat generation service
- Public browsing endpoints

See: `docs/phases/phase-2-content-management.md`
