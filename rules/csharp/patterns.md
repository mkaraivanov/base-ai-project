---
paths:
  - "**/*.cs"
---
# C# Patterns

> This file extends [common/patterns.md](../common/patterns.md) with C# specific content.

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

// Specific repository for domain entity
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}

// Implementation with EF Core
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().ToListAsync(cancellationToken);
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

    public async Task<T> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
```

## Result Pattern

Use for operations that can fail in expected ways:

```csharp
// Result type
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}

// Usage
public async Task<Result<User>> CreateUserAsync(CreateUserDto dto)
{
    var validationErrors = ValidateUser(dto);
    if (validationErrors.Any())
    {
        return Result<User>.Failure(validationErrors);
    }

    var existingUser = await _repository.GetByEmailAsync(dto.Email);
    if (existingUser is not null)
    {
        return Result<User>.Failure("User with this email already exists");
    }

    var user = new User { /* ... */ };
    await _repository.CreateAsync(user);
    return Result<User>.Success(user);
}

// API endpoint
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserDto dto)
{
    var result = await _userService.CreateUserAsync(dto);
    
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value)
        : BadRequest(new { errors = result.Errors });
}
```

## Options Pattern

Use for strongly-typed configuration:

```csharp
// Options class
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

// Registration in Program.cs
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Add validation
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Usage via injection
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
    "Secret": "your-secret-key",
    "Issuer": "your-app",
    "Audience": "your-api",
    "ExpirationMinutes": 60
  }
}
```

## Minimal API vs Controller Pattern

```csharp
// MINIMAL API - Use for simple, focused endpoints
app.MapGet("/users/{id}", async (Guid id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.WithName("GetUser")
.WithOpenApi();

app.MapPost("/users", async (CreateUserDto dto, IUserService service) =>
{
    var result = await service.CreateUserAsync(dto);
    return result.IsSuccess
        ? Results.Created($"/users/{result.Value!.Id}", result.Value)
        : Results.BadRequest(result.Errors);
})
.WithValidation();

// CONTROLLER - Use for complex endpoints with extensive model binding
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user is not null ? Ok(user) : NotFound();
    }

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

## Mediator Pattern (CQRS)

Use MediatR for clean separation of commands and queries:

```csharp
// Command
public record CreateUserCommand(string Email, string Name) : IRequest<Result<User>>;

// Command Handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<User>>
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IUserRepository repository, ILogger<CreateUserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<User>.Failure("User already exists");
        }

        var user = new User { Email = request.Email, Name = request.Name };
        await _repository.CreateAsync(user, cancellationToken);
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        return Result<User>.Success(user);
    }
}

// Query
public record GetUserByIdQuery(Guid Id) : IRequest<User?>;

// Query Handler
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}

// Usage in controller/endpoint
app.MapPost("/users", async (CreateUserDto dto, IMediator mediator) =>
{
    var command = new CreateUserCommand(dto.Email, dto.Name);
    var result = await mediator.Send(command);
    
    return result.IsSuccess
        ? Results.Created($"/users/{result.Value!.Id}", result.Value)
        : Results.BadRequest(result.Errors);
});
```

## Background Service Pattern

```csharp
// Hosted service for background work
public class EmailNotificationService : BackgroundService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailNotificationService(ILogger<EmailNotificationService> logger, IServiceScopeFactory scopeFactory)
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
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email notification service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Email notification service stopped");
    }
}

// Registration
builder.Services.AddHostedService<EmailNotificationService>();
```

## Dependency Injection Lifetimes

```csharp
// TRANSIENT - New instance every time (stateless services)
builder.Services.AddTransient<IEmailSender, EmailSender>();

// SCOPED - One instance per HTTP request (DbContext, repositories)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AppDbContext>();

// SINGLETON - One instance for application lifetime (caches, configuration)
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

// Avoid: Scoped service in singleton (causes stale data/memory leaks)
// ❌ BAD
builder.Services.AddSingleton<MyService>(); // If MyService injects AppDbContext (scoped)

// ✅ GOOD
builder.Services.AddScoped<MyService>(); // Match lifetime of dependencies
```

## API Response Format

```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];
    public PaginationMeta? Meta { get; init; }
}

public record PaginationMeta
{
    public int Total { get; init; }
    public int Page { get; init; }
    public int Limit { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
}

// Usage
[HttpGet]
public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10)
{
    var users = await _userService.GetUsersAsync(page, limit);
    var total = await _userService.GetTotalCountAsync();
    
    return Ok(new ApiResponse<List<UserDto>>
    {
        Success = true,
        Data = users,
        Meta = new PaginationMeta { Total = total, Page = page, Limit = limit }
    });
}
```
