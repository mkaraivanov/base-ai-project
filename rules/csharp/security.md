---
paths:
  - "**/*.cs"
---
# C# Security

> This file extends [common/security.md](../common/security.md) with C# specific content.

## Secret Management

```csharp
// NEVER: Hardcoded secrets
public class EmailService
{
    private const string ApiKey = "sk-1234567890abcdef"; // ❌ NEVER DO THIS
}

// ALWAYS: Environment variables
public class EmailService
{
    private readonly string _apiKey;

    public EmailService(IConfiguration configuration)
    {
        _apiKey = configuration["Email:ApiKey"] 
            ?? throw new InvalidOperationException("Email:ApiKey not configured");
    }
}

// User Secrets for development (never commit)
// dotnet user-secrets init
// dotnet user-secrets set "Email:ApiKey" "your-secret-key"

// Environment variables for production
// appsettings.json should NOT contain secrets
{
  "Email": {
    "ApiKey": "" // Set via environment variable
  }
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
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; set; }

    [Url]
    public string? Website { get; set; }
}

// Automatic validation in controllers
[ApiController]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // ModelState.IsValid automatically checked when [ApiController] is used
        var user = await _service.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

### FluentValidation

```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator(IUserRepository repository)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100)
            .MustAsync(async (email, cancellation) => 
            {
                var user = await repository.GetByEmailAsync(email, cancellation);
                return user is null;
            })
            .WithMessage("Email already exists");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 50)
            .Matches("^[a-zA-Z ]+$").WithMessage("Name can only contain letters and spaces");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120)
            .When(x => x.Age > 0);
    }
}

// Registration
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Manual validation
public class UserService
{
    private readonly IValidator<CreateUserDto> _validator;

    public async Task<Result<User>> CreateUserAsync(CreateUserDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<User>.Failure(errors);
        }

        // Create user...
    }
}
```

## SQL Injection Prevention

```csharp
// EF Core - SAFE (parameterized by default)
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// ALWAYS use parameters with raw SQL
var email = "user@example.com";
var users = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", email)
    .ToListAsync();

// Dapper - SAFE (parameterized by default)
var users = await connection.QueryAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email",
    new { Email = email });

// NEVER: String concatenation
var query = $"SELECT * FROM Users WHERE Email = '{email}'"; // ❌ SQL INJECTION!
```

## Authentication - JWT

```csharp
public class JwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// JWT configuration in Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

## Authorization

### Policy-Based Authorization

```csharp
// Define policies in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("EmailVerified", "true"));
    
    options.AddPolicy("MinimumAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));
});

// Custom requirement
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }
    public MinimumAgeRequirement(int minimumAge) => MinimumAge = minimumAge;
}

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        MinimumAgeRequirement requirement)
    {
        var ageClaim = context.User.FindFirst(c => c.Type == "Age");
        if (ageClaim != null && int.TryParse(ageClaim.Value, out var age))
        {
            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }
}

// Usage in controllers
[Authorize(Policy = "RequireAdminRole")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(Guid id) { }

// Usage in Minimal APIs
app.MapDelete("/users/{id}", async (Guid id, IUserService service) =>
{
    await service.DeleteUserAsync(id);
    return Results.NoContent();
})
.RequireAuthorization("RequireAdminRole");
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
        if (requirement.Name == "Edit" && resource.OwnerId == context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// Usage in controller
[HttpPut("{id}")]
public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentDto dto)
{
    var document = await _service.GetDocumentAsync(id);
    if (document is null) return NotFound();

    var authResult = await _authorizationService.AuthorizeAsync(
        User, document, "Edit");

    if (!authResult.Succeeded)
    {
        return Forbid();
    }

    await _service.UpdateDocumentAsync(document, dto);
    return Ok(document);
}
```

## CORS Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',') 
                ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    // More restrictive for production
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type")
            .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

app.UseCors("AllowReactApp");

// appsettings.json
{
  "Cors": {
    "AllowedOrigins": "http://localhost:3000,http://localhost:5173"
  }
}
```

## Rate Limiting (ASP.NET Core 7+)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    options.AddSlidingWindowLimiter("sliding", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.SegmentsPerWindow = 6;
    });
});

app.UseRateLimiter();

// Apply to endpoint
app.MapGet("/api/data", () => "Hello")
    .RequireRateLimiting("fixed");

// Apply to controller
[EnableRateLimiting("sliding")]
[ApiController]
public class DataController : ControllerBase { }
```

## HTTPS and Security Headers

```csharp
// Force HTTPS
app.UseHttpsRedirection();

// HSTS (HTTP Strict Transport Security)
app.UseHsts();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    
    await next();
});
```

## Data Protection API

```csharp
public class SecureDataService
{
    private readonly IDataProtector _protector;

    public SecureDataService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SecureDataService.v1");
    }

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}

// Registration
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"))
    .SetApplicationName("MyApp");
```

## Security Checklist

- [ ] No hardcoded secrets (use configuration + environment variables)
- [ ] All user inputs validated (FluentValidation or DataAnnotations)
- [ ] SQL injection prevented (EF Core parameterized queries)
- [ ] XSS prevention (proper encoding, CSP headers)
- [ ] CSRF protection enabled for cookie auth
- [ ] Authentication implemented (JWT or Cookie)
- [ ] Authorization policies configured
- [ ] Rate limiting on all public endpoints
- [ ] CORS properly configured (restrict origins)
- [ ] HTTPS enforced (UseHttpsRedirection, HSTS)
- [ ] Security headers configured
- [ ] Error messages don't leak sensitive data
- [ ] Logging excludes sensitive information

## Agent Support

- Use **security-reviewer** agent for comprehensive security audits before commits
