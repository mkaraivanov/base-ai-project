---
name: dotnet-patterns
description: ASP.NET Core architecture patterns, API design, dependency injection, middleware, configuration, and server-side best practices for .NET applications.
---

# .NET Development Patterns

Comprehensive patterns and best practices for building scalable ASP.NET Core applications.

## When to Activate

- Designing REST or GraphQL API endpoints
- Implementing repository, service, or controller layers
- Configuring dependency injection and service lifetimes
- Building middleware pipeline (authentication, logging, error handling)
- Setting up configuration with Options pattern
- Implementing background services or hosted services
- Choosing between Minimal APIs and Controllers
- Setting up SignalR for real-time features

## Minimal API vs Controller Decision Matrix

### Use Minimal APIs When:
- Simple CRUD endpoints
- Microservices with few endpoints
- Rapid prototyping
- Performance is critical (less overhead)
- Functional programming style preferred

```csharp
// Minimal API Example
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/users/{id}", async (Guid id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.WithName("GetUser")
.WithOpenApi()
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/users", async (CreateUserDto dto, IUserService service) =>
{
    var result = await service.CreateUserAsync(dto);
    return result.IsSuccess
        ? Results.Created($"/api/users/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
})
.WithValidation()
.RequireAuthorization();

app.Run();
```

### Use Controllers When:
- Complex APIs with many endpoints
- Heavy model binding and validation
- Need for action filters and middleware
- Team familiar with MVC pattern
- OpenAPI documentation with attributes

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        _logger.LogInformation("Getting user {UserId}", id);
        var user = await _userService.GetUserByIdAsync(id);
        return user is not null ? Ok(user) : NotFound();
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
    }
}
```

## Repository Pattern

```csharp
// Generic repository interface
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

// Specific repository
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);
}

// EF Core implementation
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is not null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

## Service Layer Pattern

```csharp
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is not null ? MapToDto(user) : null;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
        
        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        // Validate
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<UserDto>.Failure(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        // Check for duplicates
        var existingUser = await _repository.GetByEmailAsync(dto.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<UserDto>.Failure("User with this email already exists");
        }

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Name = dto.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(user, cancellationToken);
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        
        return Result<UserDto>.Success(MapToDto(user));
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        IsActive = user.IsActive
    };
}
```

## Options Pattern for Configuration

```csharp
// Options class
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
}

// Registration with validation
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Usage via dependency injection
public class JwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        // Use _options.Secret, _options.Issuer, etc.
    }
}

// appsettings.json
{
  "Jwt": {
    "Secret": "",  // Set via environment variable
    "Issuer": "your-app",
    "Audience": "your-api",
    "ExpirationMinutes": 60
  }
}
```

## Middleware Pipeline Pattern

```csharp
// Custom middleware
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();
        
        context.Items["RequestId"] = requestId;

        _logger.LogInformation("Request started: {Method} {Path} [{RequestId}]",
            context.Request.Method, context.Request.Path, requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {Duration}ms [{RequestId}]",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, duration, requestId);
        }
    }
}

// Extension method for registration
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}

// Middleware pipeline in Program.cs (order matters!)
app.UseHttpsRedirection();       // 1. HTTPS
app.UseCors("AllowReactApp");    // 2. CORS
app.UseRequestLogging();         // 3. Custom logging
app.UseAuthentication();         // 4. Authentication
app.UseAuthorization();          // 5. Authorization
app.UseRateLimiter();            // 6. Rate limiting

app.MapControllers();
```

## Background Service Pattern

```csharp
public class EmailNotificationService : BackgroundService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public EmailNotificationService(
        ILogger<EmailNotificationService> logger, 
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email notification service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                
                await emailService.SendPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email notification service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Email notification service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email notification service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

// Registration
builder.Services.AddHostedService<EmailNotificationService>();
```

## Dependency Injection Lifetimes

```csharp
// TRANSIENT - New instance every time (stateless services)
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<IValidator<CreateUserDto>, CreateUserDtoValidator>();

// SCOPED - One instance per HTTP request (DbContext, repositories)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// SINGLETON - One instance for application lifetime (caches, configuration)
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// ❌ AVOID: Scoped service in singleton (causes stale data/memory leaks)
// BAD: Singleton injects Scoped DbContext
builder.Services.AddSingleton<MyService>(); // If MyService injects AppDbContext

// ✅ GOOD: Match lifetime of dependencies
builder.Services.AddScoped<MyService>();
```

## Result Pattern

```csharp
public record Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}

public record Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public new static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public new static Result<T> Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
```

## Checklist

- [ ] Appropriate pattern chosen (Minimal API vs Controller)
- [ ] Repository pattern for data access
- [ ] Service layer for business logic
- [ ] Options pattern for configuration
- [ ] Proper DI lifetime scopes
- [ ] Middleware ordered correctly
- [ ] Background services use IServiceScopeFactory
- [ ] Result pattern for expected failures
- [ ] All async methods have CancellationToken
- [ ] Logging configured appropriately

## Related Resources

- See [rules/csharp/patterns.md](../../rules/csharp/patterns.md) for detailed pattern guidelines
- See [skills/efcore-patterns](../efcore-patterns/SKILL.md) for database patterns
- See [skills/dotnet-security](../dotnet-security/SKILL.md) for authentication/authorization
