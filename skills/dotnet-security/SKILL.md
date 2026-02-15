---
name: dotnet-security
description: ASP.NET Core security patterns for authentication, authorization, CORS, rate limiting, input validation, and protection against common vulnerabilities.
---

# .NET Security Patterns

Comprehensive security best practices for ASP.NET Core applications.

## When to Activate

- Implementing authentication or authorization
- Handling user input or file uploads
- Creating new API endpoints
- Working with secrets or credentials
- Storing or transmitting sensitive data
- Configuring CORS for SPAs
- Implementing rate limiting
- Adding password hashing or encryption

## Secrets Management

```csharp
// ❌ NEVER: Hardcoded secrets
public class EmailService
{
    private const string ApiKey = "sk-1234567890abcdef";  // Never do this!
    private const string Password = "password123";        // Never do this!
}

// ✅ ALWAYS: Configuration + Environment Variables
public class EmailService
{
    private readonly string _apiKey;

    public EmailService(IConfiguration configuration)
    {
        _apiKey = configuration["Email:ApiKey"]
            ?? throw new InvalidOperationException("Email:ApiKey not configured");
    }
}

// appsettings.json (no secrets!)
{
  "Email": {
    "ApiKey": ""  // Empty - set via environment variable
  }
}

// Development: User Secrets
// dotnet user-secrets init
// dotnet user-secrets set "Email:ApiKey" "your-dev-key"

// Production: Environment Variables
// Set EMAIL__APIKEY=your-prod-key in hosting environment

// Startup validation
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Validate required secrets on startup
    var requiredSettings = new[] { "Jwt:Secret", "Database:ConnectionString" };
    foreach (var setting in requiredSettings)
    {
        if (string.IsNullOrEmpty(builder.Configuration[setting]))
        {
            throw new InvalidOperationException($"Required configuration '{setting}' is missing");
        }
    }
    
    // Continue with app configuration...
}
```

## Input Validation

### Data Annotations

```csharp
public class CreateUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    public string? Website { get; set; }

    [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }
}

// Automatic validation in controllers with [ApiController]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // ModelState.IsValid is automatically checked
        // Returns 400 Bad Request if validation fails
        var user = await _service.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

### FluentValidation (Recommended)

```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private readonly IUserRepository _repository;

    public CreateUserDtoValidator(IUserRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100)
            .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 50)
            .Matches("^[a-zA-Z ]+$").WithMessage("Name can only contain letters and spaces");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120)
            .When(x => x.Age > 0);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one digit")
            .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByEmailAsync(email, cancellationToken);
        return user is null;
    }
}

// Registration
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Manual validation in service
public class UserService
{
    private readonly IValidator<CreateUserDto> _validator;

    public async Task<Result<User>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<User>.Failure(errors);
        }

        // Create user...
    }
}
```

### File Upload Validation

```csharp
public class FileUploadValidator
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg", "image/png", "image/gif", "application/pdf"
    };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public static Result ValidateFile(IFormFile file)
    {
        // Check if file exists
        if (file == null || file.Length == 0)
        {
            return Result.Failure("File is required");
        }

        // Check file size
        if (file.Length > MaxFileSize)
        {
            return Result.Failure($"File size exceeds {MaxFileSize / 1024 / 1024}MB limit");
        }

        // Check content type
        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            return Result.Failure($"File type '{file.ContentType}' is not allowed");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure($"File extension '{extension}' is not allowed");
        }

        // Verify file signature (magic bytes) to prevent MIME type spoofing
        using var reader = new BinaryReader(file.OpenReadStream());
        var signatures = new Dictionary<string, List<byte[]>>
        {
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }
        };

        if (signatures.TryGetValue(extension, out var expectedSignatures))
        {
            var headerBytes = reader.ReadBytes(8);
            var isValid = expectedSignatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));

            if (!isValid)
            {
                return Result.Failure("File content does not match the extension");
            }
        }

        return Result.Success();
    }
}

// Usage in controller
[HttpPost("upload")]
public async Task<IActionResult> UploadFile(IFormFile file)
{
    var validation = FileUploadValidator.ValidateFile(file);
    if (!validation.IsSuccess)
    {
        return BadRequest(new { error = validation.Error });
    }

    // Process file...
}
```

## SQL Injection Prevention

```csharp
// ✅ EF Core - SAFE (parameterized by default)
public async Task<List<User>> SearchUsersAsync(string searchTerm)
{
    return await _context.Users
        .Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm))
        .ToListAsync();
}

// ✅ Raw SQL with parameters - SAFE
public async Task<List<User>> SearchUsersRawAsync(string searchTerm)
{
    return await _context.Users
        .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE {0} OR Email LIKE {0}", $"%{searchTerm}%")
        .ToListAsync();
}

// ✅ Dapper with parameters - SAFE
public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
{
    return await _connection.QueryAsync<User>(
        "SELECT * FROM Users WHERE Name LIKE @SearchTerm OR Email LIKE @SearchTerm",
        new { SearchTerm = $"%{searchTerm}%" });
}

// ❌ String concatenation - SQL INJECTION VULNERABILITY!
public async Task<List<User>> SearchUsersUnsafe(string searchTerm)
{
    var query = $"SELECT * FROM Users WHERE Name LIKE '%{searchTerm}%'";  // NEVER DO THIS!
    return await _connection.QueryAsync<User>(query).ToListAsync();
}
```

## Authentication - JWT

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    [Required]
    public string Secret { get; set; } = string.Empty;
    
    [Required]
    public string Issuer { get; set; } = string.Empty;
    
    [Required]
    public string Audience { get; set; } = string.Empty;
    
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
    
    [Range(1, 10080)]  // Max 7 days
    public int RefreshTokenExpirationMinutes { get; set; } = 10080;
}

public class JwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user, IEnumerable<string> roles)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles as claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.Secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

// Configuration in Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

app.UseAuthentication();
app.UseAuthorization();
```

## Authorization

### Role-Based Authorization

```csharp
// Define policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireModeratorRole", policy => policy.RequireRole("Moderator", "Admin"));
});

// Controller level
[Authorize(Roles = "Admin")]
[ApiController]
public class AdminController : ControllerBase { }

// Action level
[Authorize(Policy = "RequireAdminRole")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(Guid id) { }

// Minimal API
app.MapDelete("/api/users/{id}", async (Guid id, IUserService service) =>
{
    await service.DeleteUserAsync(id);
    return Results.NoContent();
})
.RequireAuthorization("RequireAdminRole");
```

### Claims-Based Authorization

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("EmailVerified", "true"));
    
    options.AddPolicy("RequirePremiumSubscription", policy =>
        policy.RequireClaim("SubscriptionTier", "Premium", "Enterprise"));
});

// Usage
[Authorize(Policy = "RequireEmailVerified")]
[HttpPost("premium-feature")]
public async Task<IActionResult> AccessPremiumFeature() { }
```

### Resource-Based Authorization

```csharp
public class DocumentAuthorizationHandler : 
    AuthorizationHandler<OperationAuthorizationRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Document resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Allow owners to edit
        if (requirement.Name == "Edit" && resource.OwnerId == userId)
        {
            context.Succeed(requirement);
        }

        // Allow admins to do anything
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();

// Usage in controller
[HttpPut("{id}")]
public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentDto dto)
{
    var document = await _service.GetDocumentAsync(id);
    if (document is null) return NotFound();

    var authResult = await _authorizationService.AuthorizeAsync(User, document, "Edit");
    if (!authResult.Succeeded)
    {
        return Forbid();
    }

    await _service.UpdateDocumentAsync(document, dto);
    return Ok(document);
}
```

## Password Security

```csharp
public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        // Generate salt
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        // Hash password with salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        // Extract salt and hash from stored password
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        // Hash the provided password with the same salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        // Compare hashes
        for (int i = 0; i < HashSize; i++)
        {
            if (hashBytes[i + SaltSize] != hash[i])
            {
                return false;
            }
        }

        return true;
    }
}
```

## CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    // Development - Permissive
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    // Production - Restrictive
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type", "Accept")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithExposedHeaders("X-Pagination")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

var corsPolicy = builder.Environment.IsDevelopment() ? "Development" : "Production";
app.UseCors(corsPolicy);
```

## Rate Limiting (ASP.NET Core 7+)

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Fixed window - 10 requests per minute
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    // Sliding window - 100 requests per minute
    options.AddSlidingWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.SegmentsPerWindow = 6;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    // Token bucket - burst support
    options.AddTokenBucketLimiter("token", options =>
    {
        options.TokenLimit = 100;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        options.TokensPerPeriod = 20;
        options.AutoReplenishment = true;
    });

    // Concurrency - max concurrent requests
    options.AddConcurrencyLimiter("concurrency", options =>
    {
        options.PermitLimit = 10;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." }, 
            cancellationToken);
    };
});

app.UseRateLimiter();

// Apply to endpoint
app.MapGet("/api/public", () => "Hello")
    .RequireRateLimiting("fixed");

// Apply to controller
[EnableRateLimiting("api")]
[ApiController]
public class DataController : ControllerBase { }
```

## Security Headers

```csharp
app.Use(async (context, next) =>
{
    // Prevent MIME type sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // XSS protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Referrer policy
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");
    
    // Permissions policy
    context.Response.Headers.Add("Permissions-Policy", 
        "geolocation=(), microphone=(), camera=()");
    
    await next();
});

// HTTPS enforcement
app.UseHttpsRedirection();

// HSTS (HTTP Strict Transport Security)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
```

## Security Checklist

- [ ] No hardcoded secrets (use configuration + environment variables)
- [ ] All user inputs validated (FluentValidation or DataAnnotations)
- [ ] File uploads validated (size, type, content)
- [ ] SQL injection prevented (parameterized queries)
- [ ] XSS prevention (proper encoding, CSP headers)
- [ ] CSRF protection enabled for cookie auth
- [ ] Authentication implemented (JWT or Cookie)
- [ ] Authorization policies configured
- [ ] Passwords hashed with strong algorithm (PBKDF2, bcrypt, Argon2)
- [ ] Rate limiting on all public endpoints
- [ ] CORS properly configured (restrict origins)
- [ ] HTTPS enforced (UseHttpsRedirection, HSTS)
- [ ] Security headers configured
- [ ] Error messages don't leak sensitive data
- [ ] Logging excludes sensitive information (passwords, tokens)

## Related Resources

- See [rules/csharp/security.md](../../rules/csharp/security.md) for security guidelines
- See [skills/dotnet-patterns](../dotnet-patterns/SKILL.md) for authentication middleware
- Use [agents/security-reviewer.md](../../agents/security-reviewer.md) before commits
