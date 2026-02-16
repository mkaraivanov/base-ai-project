# Phase 6: Security & Performance Hardening

**Duration:** Week 5
**Status:** 🔵 Pending
**Complexity:** 🔴 HIGH (Production Readiness)

## Overview

This phase focuses on making the application production-ready through comprehensive security hardening, performance optimization, and quality assurance. It includes rate limiting, caching, query optimization, security audits, and comprehensive testing.

## Objectives

✅ Implement rate limiting
✅ Configure CORS properly
✅ Add input sanitization middleware
✅ Set up response caching
✅ Optimize database queries and indexes
✅ Add compression
✅ Configure structured logging
✅ Implement health checks
✅ Run security audit
✅ Perform load testing
✅ Achieve production-ready status

---

## Step 1: Rate Limiting

### 1.1 Install Package

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend
dotnet add package AspNetCoreRateLimit --version 5.0.0
```

### 1.2 Configure Rate Limiting

**File:** `Backend/appsettings.json` (add section)

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/bookings/reserve",
        "Period": "1m",
        "Limit": 10
      },
      {
        "Endpoint": "POST:/api/bookings/confirm",
        "Period": "1m",
        "Limit": 5
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

**Update Program.cs:**

```csharp
using AspNetCoreRateLimit;

// Add rate limiting services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ... after app.Build()

app.UseIpRateLimiting();
```

---

## Step 2: CORS Configuration

**Update Program.cs:**

```csharp
// CORS - Restrict to specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**File:** `Backend/appsettings.json` (add section)

```json
{
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000"
  ]
}
```

**File:** `Backend/appsettings.Production.json`

```json
{
  "AllowedOrigins": [
    "https://your-production-frontend.com"
  ]
}
```

---

## Step 3: Input Validation Middleware

**File:** `Backend/Middleware/InputSanitizationMiddleware.cs`

```csharp
using System.Text.RegularExpressions;

namespace Backend.Middleware;

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for SQL injection patterns in query strings
        foreach (var query in context.Request.Query)
        {
            if (ContainsSqlInjection(query.Value.ToString()))
            {
                _logger.LogWarning("Potential SQL injection detected in query: {Key}", query.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid input detected" });
                return;
            }
        }

        // Check for XSS patterns in headers
        foreach (var header in context.Request.Headers)
        {
            if (ContainsXss(header.Value.ToString()))
            {
                _logger.LogWarning("Potential XSS detected in header: {Key}", header.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid input detected" });
                return;
            }
        }

        await _next(context);
    }

    private static bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var sqlPatterns = new[]
        {
            @"(\bOR\b|\bAND\b).*=.*",
            @"(';|\";|--|\#|\/\*|\*\/)",
            @"\bDROP\b.*\bTABLE\b",
            @"\bEXEC\b|\bEXECUTE\b",
            @"\bUNION\b.*\bSELECT\b"
        };

        return sqlPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private static bool ContainsXss(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var xssPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"on\w+\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>"
        };

        return xssPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }
}
```

**Update Program.cs:**

```csharp
app.UseMiddleware<InputSanitizationMiddleware>();
```

---

## Step 4: Response Caching

### 4.1 Configure Caching Service

**File:** `Backend/Infrastructure/Caching/ICacheService.cs`

```csharp
namespace Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Caching/MemoryCacheService.cs`

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

**Update Program.cs:**

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
```

### 4.2 Use Caching in Services

**Update MovieService:**

```csharp
public class MovieService : IMovieService
{
    private readonly ICacheService _cacheService;
    private const string MoviesCacheKey = "movies:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"{MoviesCacheKey}:{activeOnly}";
            var cached = await _cacheService.GetAsync<List<MovieDto>>(cacheKey, ct);
            if (cached is not null)
            {
                return Result<List<MovieDto>>.Success(cached);
            }

            // Fetch from database
            var movies = await _movieRepository.GetAllAsync(activeOnly, ct);
            var dtos = movies.Select(MapToDto).ToList();

            // Cache the result
            await _cacheService.SetAsync(cacheKey, dtos, CacheDuration, ct);

            return Result<List<MovieDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movies");
            return Result<List<MovieDto>>.Failure("Failed to retrieve movies");
        }
    }
}
```

---

## Step 5: Database Optimization

### 5.1 Verify Indexes

Run the following SQL to ensure all critical indexes exist:

```sql
-- Seat availability queries
CREATE NONCLUSTERED INDEX IX_Seats_Showtime_Status
ON Seats(ShowtimeId, Status)
INCLUDE (SeatNumber, Price, SeatType);

-- Booking queries
CREATE NONCLUSTERED INDEX IX_Bookings_User_Status
ON Bookings(UserId, Status)
INCLUDE (BookingNumber, ShowtimeId, BookedAt);

-- Reservation expiration queries
CREATE NONCLUSTERED INDEX IX_Reservations_Expires_Status
ON Reservations(ExpiresAt, Status)
WHERE Status = 0; -- Pending only

-- Showtime queries
CREATE NONCLUSTERED INDEX IX_Showtimes_StartTime_Hall
ON Showtimes(StartTime, CinemaHallId, IsActive)
INCLUDE (MovieId, BasePrice);

-- User lookup
CREATE NONCLUSTERED INDEX IX_Users_Email
ON Users(Email)
WHERE IsActive = 1;
```

### 5.2 Query Optimization Guidelines

Update repositories to use optimized queries:

```csharp
// Use AsNoTracking for read-only queries
var movies = await _context.Movies
    .AsNoTracking()
    .Where(m => m.IsActive)
    .ToListAsync(ct);

// Use projections to reduce data transfer
var movieSummaries = await _context.Movies
    .AsNoTracking()
    .Where(m => m.IsActive)
    .Select(m => new MovieSummaryDto(m.Id, m.Title, m.PosterUrl))
    .ToListAsync(ct);

// Use Include for eager loading (prevent N+1)
var bookings = await _context.Bookings
    .AsNoTracking()
    .Include(b => b.Showtime)
        .ThenInclude(s => s.Movie)
    .Where(b => b.UserId == userId)
    .ToListAsync(ct);
```

---

## Step 6: Response Compression

**Update Program.cs:**

```csharp
using Microsoft.AspNetCore.ResponseCompression;

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// ... after app.Build()

app.UseResponseCompression();
```

---

## Step 7: Security Headers

**File:** `Backend/Middleware/SecurityHeadersMiddleware.cs`

```csharp
namespace Backend.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME-sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // XSS Protection
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Content Security Policy
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");

        // Referrer Policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=()");

        await _next(context);
    }
}
```

**Update Program.cs:**

```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## Step 8: Structured Logging

**Update Program.cs:**

```csharp
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CinemaBooking")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/cinema-booking-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
```

---

## Step 9: Health Checks

**Update Program.cs:**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CinemaDbContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy());

// ... after app.Build()

app.MapHealthChecks("/health", new()
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

---

## Step 10: Global Exception Handler

**File:** `Backend/Middleware/GlobalExceptionHandler.cs`

```csharp
using System.Net;
using Backend.Models;

namespace Backend.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiResponse<object>(
            false,
            null,
            _environment.IsDevelopment()
                ? $"{exception.Message} | {exception.StackTrace}"
                : "An internal server error occurred. Please try again later."
        );

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

**Update Program.cs:**

```csharp
app.UseMiddleware<GlobalExceptionHandler>();
```

---

## Step 11: Security Audit

### 11.1 Run Security Reviewer Agent

```bash
# Use the security-reviewer agent to scan the codebase
# This should be done via Claude Code CLI
```

### 11.2 Security Checklist

- [ ] No hardcoded secrets (check appsettings.json, environment variables)
- [ ] All passwords hashed with BCrypt
- [ ] JWT tokens validated correctly
- [ ] SQL injection prevented (EF Core parameterization)
- [ ] XSS prevented (JSON responses, no HTML rendering)
- [ ] CSRF protection (SameSite cookies if using cookies)
- [ ] Rate limiting enabled on all endpoints
- [ ] CORS configured with specific origins
- [ ] HTTPS enforced in production
- [ ] Security headers configured
- [ ] Input validation on all endpoints
- [ ] Error messages don't leak sensitive information
- [ ] Authentication required on protected endpoints
- [ ] Authorization checks for admin-only endpoints

---

## Step 12: Load Testing

### 12.1 Install k6 (Load Testing Tool)

```bash
brew install k6  # macOS
# or
choco install k6  # Windows
```

### 12.2 Create Load Test Script

**File:** `tests/load/booking-flow.js`

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 50 },  // Ramp up to 50 users
    { duration: '3m', target: 50 },  // Stay at 50 users
    { duration: '1m', target: 100 }, // Ramp up to 100 users
    { duration: '3m', target: 100 }, // Stay at 100 users
    { duration: '1m', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.01'],   // Error rate should be below 1%
  },
};

const BASE_URL = 'http://localhost:5000/api';

export default function () {
  // Test seat availability query
  const showtimeId = 'your-test-showtime-id';
  const response = http.get(`${BASE_URL}/bookings/availability/${showtimeId}`);

  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
```

### 12.3 Run Load Test

```bash
k6 run tests/load/booking-flow.js
```

---

## Step 13: Performance Benchmarks

### Target Metrics

- **Seat Availability Query**: < 200ms (p95)
- **Create Reservation**: < 500ms (p95)
- **Confirm Booking**: < 1000ms (p95) including payment delay
- **Movie Listing** (cached): < 100ms (p95)
- **Database Connection Pool**: Min 5, Max 100
- **Memory Usage**: < 500MB under load
- **CPU Usage**: < 70% under load

### Connection String Optimization

**File:** `Backend/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CinemaBookingDb;Trusted_Connection=true;TrustServerCertificate=true;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30"
  }
}
```

---

## Step 14: Production Checklist

### Pre-Deployment

- [ ] All secrets moved to environment variables or Azure Key Vault
- [ ] Connection strings secured
- [ ] JWT secret key is strong (32+ characters)
- [ ] HTTPS enforced
- [ ] Database migrations tested
- [ ] Logging configured for production
- [ ] Health checks working
- [ ] Rate limiting configured
- [ ] CORS configured for production domain
- [ ] Security headers enabled
- [ ] Response compression enabled
- [ ] Database indexes created
- [ ] Caching configured
- [ ] Error handling comprehensive
- [ ] Load testing passed
- [ ] Security audit passed
- [ ] Code review completed
- [ ] Test coverage 80%+

### Deployment Steps

1. Run database migrations in production
2. Update environment variables
3. Deploy backend application
4. Deploy frontend application
5. Verify health checks
6. Monitor logs for errors
7. Test critical flows end-to-end

---

## Verification Checklist

- [ ] Rate limiting blocks excessive requests
- [ ] CORS only allows configured origins
- [ ] Security headers present in responses
- [ ] Input sanitization catches malicious input
- [ ] Caching reduces database load
- [ ] Compression reduces response size
- [ ] Health checks return 200
- [ ] Load testing meets performance benchmarks
- [ ] Security audit passes with no CRITICAL/HIGH issues
- [ ] Global exception handler catches unhandled exceptions
- [ ] Structured logging works correctly
- [ ] Database queries optimized (no N+1 issues)

---

## Common Issues & Solutions

### Issue 1: Rate limiting not working
**Solution:** Ensure `AddMemoryCache()` is called before `AddInMemoryRateLimiting()`.

### Issue 2: CORS errors in production
**Solution:** Verify `AllowedOrigins` in appsettings.Production.json matches frontend domain exactly.

### Issue 3: Slow seat availability queries
**Solution:** Verify index `IX_Seats_Showtime_Status` exists and is used (check execution plan).

### Issue 4: Memory leaks under load
**Solution:** Ensure DbContext is scoped, not singleton. Verify caching doesn't store too much data.

---

## Performance Optimization Summary

1. **Database**: Indexes on all foreign keys and frequently queried columns
2. **Caching**: Movies (5 min), Halls (1 hour), No cache on seat availability
3. **Queries**: AsNoTracking for reads, Include for eager loading, projections for summaries
4. **Compression**: Brotli/Gzip on all JSON responses
5. **Rate Limiting**: 10 req/min for bookings, 100 req/min for reads
6. **Connection Pooling**: Min 5, Max 100 connections

---

## Next Steps

✅ **Phase 6 Complete!**

**All 6 phases are now complete!**

### Post-Implementation Tasks

1. **Documentation Review**: Update README with deployment instructions
2. **Architecture Diagrams**: Create system architecture and data flow diagrams
3. **API Documentation**: Ensure Swagger/OpenAPI docs are complete
4. **User Guide**: Create end-user documentation
5. **Monitoring Setup**: Configure Application Insights or equivalent
6. **Backup Strategy**: Set up database backup schedule
7. **Disaster Recovery**: Document recovery procedures

### Production Deployment

1. Choose hosting provider (Azure, AWS, etc.)
2. Set up CI/CD pipeline
3. Configure production database
4. Deploy and monitor
5. Celebrate! 🎉

---

**Congratulations! You have successfully completed all 6 phases of the Cinema Ticket Booking System implementation.**
