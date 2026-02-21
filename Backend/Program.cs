using System.Text;
using Application.Services;
using Application.Validators;
using AspNetCoreRateLimit;
using Backend.Endpoints;
using Backend.Infrastructure.Caching;
using Backend.Middleware;
using FluentValidation;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

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

// Add services to the container.
builder.Services.AddOpenApi();

// Localization – no ResourcesPath so the factory resolves embedded resources
// from each assembly's satellite .resources.dll (Application.resources.dll, etc.)
builder.Services.AddLocalization();

// Memory Cache
builder.Services.AddMemoryCache();

// Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Caching
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// HTTP context accessor (required by AuditInterceptor)
builder.Services.AddHttpContextAccessor();

// Audit interceptor (scoped so it can access IHttpContextAccessor per-request)
builder.Services.AddScoped<AuditInterceptor>();

// Response Compression
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

// Database
builder.Services.AddDbContext<CinemaDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<ICinemaRepository, CinemaRepository>();
builder.Services.AddScoped<ICinemaHallRepository, CinemaHallRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IReservationTicketRepository, ReservationTicketRepository>();
builder.Services.AddScoped<IBookingTicketRepository, BookingTicketRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<IReportingRepository, ReportingRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<ICinemaHallService, CinemaHallService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton(TimeProvider.System);

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Background Services
builder.Services.AddHostedService<ExpiredReservationCleanupService>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// CORS - Restrict to specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Try to get from Cors:AllowedOrigins (Development), then AllowedOrigins (default), fallback to 5173/5174
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
            ?? builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:5174" };

        // Always add the current frontend dev port if not present
        var frontendDevPorts = new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:4173" };
        allowedOrigins = allowedOrigins.Union(frontendDevPorts).ToArray();

        // Log the allowed origins at startup
        Console.WriteLine($"[CORS] Allowed origins: {string.Join(", ", allowedOrigins)}");

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CinemaDbContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    Infrastructure.Data.AdminSeeder.SeedAdminUserAsync(db).GetAwaiter().GetResult();
    Infrastructure.Data.TestDataSeeder.SeedTestDataAsync(db).GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware pipeline (order matters)
app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<InputSanitizationMiddleware>();
app.UseIpRateLimiting();
app.UseResponseCompression();

// Request Localization
var supportedCultures = new[] { "en", "bg" };
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;
});

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

// Map endpoints
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
}).AllowAnonymous();

app.MapGroup("/api/auth")
    .MapAuthEndpoints()
    .WithTags("Authentication");

app.MapGroup("/api/cinemas")
    .MapCinemaEndpoints()
    .WithTags("Cinemas");

app.MapGroup("/api/movies")
    .MapMovieEndpoints()
    .WithTags("Movies");

app.MapGroup("/api/halls")
    .MapCinemaHallEndpoints()
    .WithTags("Cinema Halls");

app.MapGroup("/api/showtimes")
    .MapShowtimeEndpoints()
    .WithTags("Showtimes");

app.MapGroup("/api/bookings")
    .MapBookingEndpoints()
    .WithTags("Bookings");

app.MapGroup("/api/loyalty")
    .MapLoyaltyEndpoints()
    .WithTags("Loyalty");

app.MapGroup("/api/ticket-types")
    .MapTicketTypeEndpoints()
    .WithTags("Ticket Types");

app.MapGroup("/api/reports")
    .MapReportingEndpoints()
    .WithTags("Reports");

app.MapGroup("/api/audit")
    .MapAuditEndpoints()
    .WithTags("Audit");

app.Run();

// Make Program accessible to tests
public partial class Program { }
