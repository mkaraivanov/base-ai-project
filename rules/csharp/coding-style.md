---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C# Coding Style

> This file extends [common/coding-style.md](../common/coding-style.md) with C# specific content.

## Immutability

Use records and init-only properties for immutable data:

```csharp
// WRONG: Mutation
public void UpdateUser(User user, string name)
{
    user.Name = name;  // MUTATION!
    return user;
}

// CORRECT: Immutability with records
public record User(string Id, string Name, string Email);

public User UpdateUser(User user, string name)
{
    return user with { Name = name };
}

// CORRECT: Immutability with init-only properties
public class User
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
}
```

## Nullable Reference Types (NRT)

ALWAYS enable NRT and handle nullability explicitly:

```csharp
// In .csproj
<Nullable>enable</Nullable>

// Code
public string GetUserName(User? user)
{
    if (user is null)
    {
        return "Unknown";
    }
    
    return user.Name; // Safe, null checked
}

// Use null-forgiving operator only when certain
public string GetRequiredConfig(IConfiguration config)
{
    return config["ApiKey"]!; // Only if you're certain it exists
}
```

## Async/Await Best Practices

```csharp
// WRONG: async void (except event handlers)
public async void ProcessData() { } // ❌

// CORRECT: async Task
public async Task ProcessDataAsync() { } // ✅

// CORRECT: Use CancellationToken
public async Task<User> GetUserAsync(string id, CancellationToken cancellationToken = default)
{
    return await _httpClient.GetFromJsonAsync<User>($"/users/{id}", cancellationToken);
}

// CORRECT: Use ConfigureAwait(false) in library code
public async Task<Data> GetDataAsync()
{
    var response = await _httpClient.GetAsync("/data").ConfigureAwait(false);
    return await response.Content.ReadFromJsonAsync<Data>().ConfigureAwait(false);
}

// ASP.NET Core code - no ConfigureAwait needed (no synchronization context)
public async Task<IActionResult> GetUser(string id)
{
    var user = await _userService.GetUserAsync(id);
    return Ok(user);
}
```

## LINQ Best Practices

```csharp
// WRONG: Multiple enumeration
var users = GetUsers().Where(u => u.IsActive);
var count = users.Count(); // First enumeration
var list = users.ToList(); // Second enumeration - wasteful!

// CORRECT: Single enumeration
var users = GetUsers().Where(u => u.IsActive).ToList();
var count = users.Count;

// CORRECT: Use Any() for existence checks
if (users.Any()) { } // ✅ Efficient

// WRONG: Count() for existence checks
if (users.Count() > 0) { } // ❌ Enumerates entire collection
```

## Pattern Matching

Use modern pattern matching for cleaner code:

```csharp
// Property patterns
public decimal GetDiscount(Customer customer) => customer switch
{
    { IsPremium: true, YearsActive: > 5 } => 0.20m,
    { IsPremium: true } => 0.10m,
    { YearsActive: > 3 } => 0.05m,
    _ => 0m
};

// Type patterns with declarations
public string Describe(object obj) => obj switch
{
    int i => $"Integer: {i}",
    string s when s.Length > 10 => "Long string",
    string s => $"String: {s}",
    null => "Null",
    _ => "Unknown"
};

// Positional patterns with records
public record Point(int X, int Y);

public string GetQuadrant(Point point) => point switch
{
    (0, 0) => "Origin",
    (> 0, > 0) => "Quadrant I",
    (< 0, > 0) => "Quadrant II",
    _ => "Other"
};
```

## Naming Conventions

```csharp
// PascalCase for public members
public class UserService
{
    public string FirstName { get; set; }
    public void ProcessData() { }
}

// camelCase with underscore for private fields
public class UserRepository
{
    private readonly IDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    
    public UserRepository(IDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
}

// Async suffix for async methods
public async Task<User> GetUserAsync(string id) { }
public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { }
```

## Modern C# Features

```csharp
// File-scoped namespaces (C# 10+)
namespace MyApp.Services;

public class UserService { }

// Global usings (in separate GlobalUsings.cs or .csproj)
global using System;
global using System.Linq;
global using System.Threading.Tasks;

// Top-level statements (Program.cs)
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();

// Raw string literals (C# 11+)
var json = """
    {
        "name": "John",
        "age": 30
    }
    """;

// Collection expressions (C# 12+)
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
```

## Error Handling

```csharp
// ALWAYS handle specific exceptions
try
{
    var result = await _service.ProcessAsync(data);
    return result;
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for {Data}", data);
    return BadRequest(new { error = ex.Message });
}
catch (NotFoundException ex)
{
    _logger.LogInformation("Resource not found: {Message}", ex.Message);
    return NotFound();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing data");
    return StatusCode(500, new { error = "An error occurred processing your request" });
}

// Use Result pattern for expected failures
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

## Code Quality Checklist

Before marking work complete:
- [ ] Nullable reference types enabled and properly annotated
- [ ] No `async void` methods (except event handlers)
- [ ] CancellationToken parameters on async methods
- [ ] No multiple LINQ enumerations
- [ ] Pattern matching used where appropriate
- [ ] Records used for immutable data
- [ ] Private fields use `_camelCase`
- [ ] Async methods have `Async` suffix
- [ ] File-scoped namespaces used
- [ ] Proper exception handling with logging
