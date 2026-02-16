# Phase 2: Content Management (Movies, Halls, Showtimes)

**Duration:** Week 1-2
**Status:** 🔵 Pending

## Overview

This phase implements the core content management system for the cinema. Admin users will be able to manage movies, cinema halls with flexible seat layouts, and showtimes. Public users will be able to browse movies and available showtimes. The phase includes automatic seat generation when showtimes are created.

## Objectives

✅ Implement Movie entity with CRUD operations
✅ Implement CinemaHall entity with JSON-based seat layout
✅ Implement Showtime entity with scheduling logic
✅ Implement Seat entity (auto-generated per showtime)
✅ Create admin endpoints for content management
✅ Create public browsing endpoints
✅ Implement business rules (no overlapping showtimes)
✅ Build seat generation service
✅ Achieve 80%+ test coverage

---

## Step 1: Implement Domain Entities

### 1.1 Create Movie Entity

**File:** `Backend/Domain/Entities/Movie.cs`

```csharp
namespace Domain.Entities;

public class Movie
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Genre { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string Rating { get; init; } = string.Empty; // PG, PG-13, R, etc.
    public string PosterUrl { get; init; } = string.Empty;
    public DateOnly ReleaseDate { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

### 1.2 Create CinemaHall Entity

**File:** `Backend/Domain/Entities/CinemaHall.cs`

```csharp
namespace Domain.Entities;

public class CinemaHall
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public string SeatLayoutJson { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
}
```

### 1.3 Create Seat Layout Value Objects

**File:** `Backend/Domain/ValueObjects/SeatLayout.cs`

```csharp
namespace Domain.ValueObjects;

public record SeatLayout
{
    public int Rows { get; init; }
    public int SeatsPerRow { get; init; }
    public List<SeatDefinition> Seats { get; init; } = new();
}

public record SeatDefinition
{
    public string SeatNumber { get; init; } = string.Empty; // "A1", "B5", etc.
    public int Row { get; init; }
    public int Column { get; init; }
    public string SeatType { get; init; } = "Regular"; // Regular, Premium, VIP
    public decimal PriceMultiplier { get; init; } = 1.0m;
    public bool IsAvailable { get; init; } = true; // For broken/maintenance seats
}
```

### 1.4 Create Showtime Entity

**File:** `Backend/Domain/Entities/Showtime.cs`

```csharp
namespace Domain.Entities;

public class Showtime
{
    public Guid Id { get; init; }
    public Guid MovieId { get; init; }
    public Guid CinemaHallId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public decimal BasePrice { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }

    // Navigation properties
    public Movie? Movie { get; init; }
    public CinemaHall? CinemaHall { get; init; }
}
```

### 1.5 Create Seat Entity

**File:** `Backend/Domain/Entities/Seat.cs`

```csharp
namespace Domain.Entities;

public class Seat
{
    public Guid Id { get; init; }
    public Guid ShowtimeId { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public string SeatType { get; init; } = "Regular";
    public decimal Price { get; init; }
    public SeatStatus Status { get; init; } = SeatStatus.Available;
    public Guid? ReservationId { get; init; }
    public DateTime? ReservedUntil { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>(); // Optimistic concurrency

    // Navigation
    public Showtime? Showtime { get; init; }
}

public enum SeatStatus
{
    Available = 0,
    Reserved = 1,
    Booked = 2,
    Blocked = 3
}
```

---

## Step 2: Update DbContext

**File:** `Backend/Infrastructure/Data/CinemaDbContext.cs`

Add the following to your existing `CinemaDbContext`:

```csharp
public DbSet<Movie> Movies => Set<Movie>();
public DbSet<CinemaHall> CinemaHalls => Set<CinemaHall>();
public DbSet<Showtime> Showtimes => Set<Showtime>();
public DbSet<Seat> Seats => Set<Seat>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ... existing User configuration ...

    // Movie configuration
    modelBuilder.Entity<Movie>(entity =>
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.Description)
            .HasMaxLength(2000);

        entity.Property(e => e.Genre)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.Rating)
            .IsRequired()
            .HasMaxLength(10);

        entity.Property(e => e.PosterUrl)
            .HasMaxLength(500);

        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.Genre);
    });

    // CinemaHall configuration
    modelBuilder.Entity<CinemaHall>(entity =>
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.SeatLayoutJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        entity.HasIndex(e => e.IsActive);
    });

    // Showtime configuration
    modelBuilder.Entity<Showtime>(entity =>
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.BasePrice)
            .HasPrecision(10, 2);

        entity.HasOne(e => e.Movie)
            .WithMany()
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CinemaHall)
            .WithMany()
            .HasForeignKey(e => e.CinemaHallId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.StartTime, e.CinemaHallId });
        entity.HasIndex(e => e.MovieId);
        entity.HasIndex(e => e.IsActive);
    });

    // Seat configuration (CRITICAL for concurrency)
    modelBuilder.Entity<Seat>(entity =>
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.SeatNumber)
            .IsRequired()
            .HasMaxLength(10);

        entity.Property(e => e.Price)
            .HasPrecision(10, 2);

        entity.Property(e => e.SeatType)
            .IsRequired()
            .HasMaxLength(20);

        // Optimistic concurrency control
        entity.Property(e => e.RowVersion)
            .IsRowVersion();

        entity.HasOne(e => e.Showtime)
            .WithMany()
            .HasForeignKey(e => e.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint for seat per showtime
        entity.HasIndex(e => new { e.ShowtimeId, e.SeatNumber })
            .IsUnique();

        // Index for fast availability queries
        entity.HasIndex(e => new { e.ShowtimeId, e.Status });
    });
}
```

---

## Step 3: Create DTOs

### 3.1 Movie DTOs

**File:** `Backend/Application/DTOs/Movies/CreateMovieDto.cs`

```csharp
namespace Application.DTOs.Movies;

public record CreateMovieDto(
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate
);
```

**File:** `Backend/Application/DTOs/Movies/UpdateMovieDto.cs`

```csharp
namespace Application.DTOs.Movies;

public record UpdateMovieDto(
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate,
    bool IsActive
);
```

**File:** `Backend/Application/DTOs/Movies/MovieDto.cs`

```csharp
namespace Application.DTOs.Movies;

public record MovieDto(
    Guid Id,
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate,
    bool IsActive,
    DateTime CreatedAt
);
```

### 3.2 Cinema Hall DTOs

**File:** `Backend/Application/DTOs/CinemaHalls/CreateCinemaHallDto.cs`

```csharp
using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CreateCinemaHallDto(
    string Name,
    SeatLayout SeatLayout
);
```

**File:** `Backend/Application/DTOs/CinemaHalls/CinemaHallDto.cs`

```csharp
using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CinemaHallDto(
    Guid Id,
    string Name,
    int TotalSeats,
    SeatLayout SeatLayout,
    bool IsActive,
    DateTime CreatedAt
);
```

### 3.3 Showtime DTOs

**File:** `Backend/Application/DTOs/Showtimes/CreateShowtimeDto.cs`

```csharp
namespace Application.DTOs.Showtimes;

public record CreateShowtimeDto(
    Guid MovieId,
    Guid CinemaHallId,
    DateTime StartTime,
    decimal BasePrice
);
```

**File:** `Backend/Application/DTOs/Showtimes/ShowtimeDto.cs`

```csharp
using Application.DTOs.Movies;

namespace Application.DTOs.Showtimes;

public record ShowtimeDto(
    Guid Id,
    Guid MovieId,
    string MovieTitle,
    Guid CinemaHallId,
    string HallName,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice,
    int AvailableSeats,
    bool IsActive
);
```

---

## Step 4: Create Validators

**File:** `Backend/Application/Validators/CreateMovieDtoValidator.cs`

```csharp
using Application.DTOs.Movies;
using FluentValidation;

namespace Application.Validators;

public class CreateMovieDtoValidator : AbstractValidator<CreateMovieDto>
{
    public CreateMovieDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage("Genre is required")
            .MaximumLength(50).WithMessage("Genre must not exceed 50 characters");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(500).WithMessage("Duration must not exceed 500 minutes");

        RuleFor(x => x.Rating)
            .NotEmpty().WithMessage("Rating is required")
            .Must(r => new[] { "G", "PG", "PG-13", "R", "NC-17" }.Contains(r))
            .WithMessage("Rating must be G, PG, PG-13, R, or NC-17");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Poster URL is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Poster URL must be a valid URL");

        RuleFor(x => x.ReleaseDate)
            .NotEmpty().WithMessage("Release date is required");
    }
}
```

**File:** `Backend/Application/Validators/CreateShowtimeDtoValidator.cs`

```csharp
using Application.DTOs.Showtimes;
using FluentValidation;

namespace Application.Validators;

public class CreateShowtimeDtoValidator : AbstractValidator<CreateShowtimeDto>
{
    public CreateShowtimeDtoValidator()
    {
        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("Movie ID is required");

        RuleFor(x => x.CinemaHallId)
            .NotEmpty().WithMessage("Cinema Hall ID is required");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Start time must be in the future");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Base price must not exceed 1000");
    }
}
```

---

## Step 5: Create Repositories

### 5.1 Movie Repository

**File:** `Backend/Infrastructure/Repositories/IMovieRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IMovieRepository
{
    Task<List<Movie>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Movie> CreateAsync(Movie movie, CancellationToken ct = default);
    Task<Movie> UpdateAsync(Movie movie, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/MovieRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly CinemaDbContext _context;

    public MovieRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Movie>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Movies.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync(ct);
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Movie> CreateAsync(Movie movie, CancellationToken ct = default)
    {
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(ct);
        return movie;
    }

    public async Task<Movie> UpdateAsync(Movie movie, CancellationToken ct = default)
    {
        _context.Movies.Update(movie);
        await _context.SaveChangesAsync(ct);
        return movie;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var movie = await _context.Movies.FindAsync(new object[] { id }, ct);
        if (movie is not null)
        {
            // Soft delete
            var updated = movie with { IsActive = false };
            _context.Movies.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

### 5.2 CinemaHall Repository

**File:** `Backend/Infrastructure/Repositories/ICinemaHallRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ICinemaHallRepository
{
    Task<List<CinemaHall>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<CinemaHall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CinemaHall> CreateAsync(CinemaHall hall, CancellationToken ct = default);
    Task<CinemaHall> UpdateAsync(CinemaHall hall, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/CinemaHallRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CinemaHallRepository : ICinemaHallRepository
{
    private readonly CinemaDbContext _context;

    public CinemaHallRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<CinemaHall>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.CinemaHalls.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        return await query.OrderBy(h => h.Name).ToListAsync(ct);
    }

    public async Task<CinemaHall?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CinemaHalls
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task<CinemaHall> CreateAsync(CinemaHall hall, CancellationToken ct = default)
    {
        _context.CinemaHalls.Add(hall);
        await _context.SaveChangesAsync(ct);
        return hall;
    }

    public async Task<CinemaHall> UpdateAsync(CinemaHall hall, CancellationToken ct = default)
    {
        _context.CinemaHalls.Update(hall);
        await _context.SaveChangesAsync(ct);
        return hall;
    }
}
```

### 5.3 Showtime Repository

**File:** `Backend/Infrastructure/Repositories/IShowtimeRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IShowtimeRepository
{
    Task<List<Showtime>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<List<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct = default);
    Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Showtime> CreateAsync(Showtime showtime, CancellationToken ct = default);
    Task<Showtime> UpdateAsync(Showtime showtime, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasOverlappingShowtimeAsync(Guid hallId, DateTime startTime, DateTime endTime, Guid? excludeShowtimeId = null, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/ShowtimeRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShowtimeRepository : IShowtimeRepository
{
    private readonly CinemaDbContext _context;

    public ShowtimeRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Showtime>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .Where(s => s.IsActive);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartTime <= toDate.Value);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct = default)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .Where(s => s.MovieId == movieId && s.IsActive)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Showtime> CreateAsync(Showtime showtime, CancellationToken ct = default)
    {
        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task<Showtime> UpdateAsync(Showtime showtime, CancellationToken ct = default)
    {
        _context.Showtimes.Update(showtime);
        await _context.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var showtime = await _context.Showtimes.FindAsync(new object[] { id }, ct);
        if (showtime is not null)
        {
            var updated = showtime with { IsActive = false };
            _context.Showtimes.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> HasOverlappingShowtimeAsync(
        Guid hallId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeShowtimeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Where(s => s.CinemaHallId == hallId && s.IsActive);

        if (excludeShowtimeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeShowtimeId.Value);
        }

        return await query.AnyAsync(s =>
            (startTime >= s.StartTime && startTime < s.EndTime) ||
            (endTime > s.StartTime && endTime <= s.EndTime) ||
            (startTime <= s.StartTime && endTime >= s.EndTime), ct);
    }
}
```

---

## Step 6: Create Services

### 6.1 Movie Service

**File:** `Backend/Application/Services/IMovieService.cs`

```csharp
using Application.DTOs.Movies;
using Domain.Common;

namespace Application.Services;

public interface IMovieService
{
    Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Result<MovieDto>> GetMovieByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<MovieDto>> CreateMovieAsync(CreateMovieDto dto, CancellationToken ct = default);
    Task<Result<MovieDto>> UpdateMovieAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default);
    Task<Result> DeleteMovieAsync(Guid id, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Services/MovieService.cs`

```csharp
using Application.DTOs.Movies;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<MovieService> _logger;
    private readonly TimeProvider _timeProvider;

    public MovieService(
        IMovieRepository movieRepository,
        ILogger<MovieService> logger,
        TimeProvider? timeProvider = null)
    {
        _movieRepository = movieRepository;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        try
        {
            var movies = await _movieRepository.GetAllAsync(activeOnly, ct);
            var dtos = movies.Select(MapToDto).ToList();
            return Result<List<MovieDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movies");
            return Result<List<MovieDto>>.Failure("Failed to retrieve movies");
        }
    }

    public async Task<Result<MovieDto>> GetMovieByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var movie = await _movieRepository.GetByIdAsync(id, ct);
            if (movie is null)
            {
                return Result<MovieDto>.Failure("Movie not found");
            }

            return Result<MovieDto>.Success(MapToDto(movie));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie {MovieId}", id);
            return Result<MovieDto>.Failure("Failed to retrieve movie");
        }
    }

    public async Task<Result<MovieDto>> CreateMovieAsync(CreateMovieDto dto, CancellationToken ct = default)
    {
        try
        {
            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Genre = dto.Genre,
                DurationMinutes = dto.DurationMinutes,
                Rating = dto.Rating,
                PosterUrl = dto.PosterUrl,
                ReleaseDate = dto.ReleaseDate,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().DateTime,
                UpdatedAt = _timeProvider.GetUtcNow().DateTime
            };

            var created = await _movieRepository.CreateAsync(movie, ct);
            _logger.LogInformation("Movie created: {MovieId} - {Title}", created.Id, created.Title);

            return Result<MovieDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating movie: {Title}", dto.Title);
            return Result<MovieDto>.Failure("Failed to create movie");
        }
    }

    public async Task<Result<MovieDto>> UpdateMovieAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result<MovieDto>.Failure("Movie not found");
            }

            var updated = existing with
            {
                Title = dto.Title,
                Description = dto.Description,
                Genre = dto.Genre,
                DurationMinutes = dto.DurationMinutes,
                Rating = dto.Rating,
                PosterUrl = dto.PosterUrl,
                ReleaseDate = dto.ReleaseDate,
                IsActive = dto.IsActive,
                UpdatedAt = _timeProvider.GetUtcNow().DateTime
            };

            var result = await _movieRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Movie updated: {MovieId}", id);

            return Result<MovieDto>.Success(MapToDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating movie {MovieId}", id);
            return Result<MovieDto>.Failure("Failed to update movie");
        }
    }

    public async Task<Result> DeleteMovieAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure("Movie not found");
            }

            await _movieRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Movie deleted: {MovieId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting movie {MovieId}", id);
            return Result.Failure("Failed to delete movie");
        }
    }

    private static MovieDto MapToDto(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Description,
        movie.Genre,
        movie.DurationMinutes,
        movie.Rating,
        movie.PosterUrl,
        movie.ReleaseDate,
        movie.IsActive,
        movie.CreatedAt
    );
}
```

### 6.2 Showtime Service

**File:** `Backend/Application/Services/IShowtimeService.cs`

```csharp
using Application.DTOs.Showtimes;
using Domain.Common;

namespace Application.Services;

public interface IShowtimeService
{
    Task<Result<List<ShowtimeDto>>> GetShowtimesAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<Result<List<ShowtimeDto>>> GetShowtimesByMovieAsync(Guid movieId, CancellationToken ct = default);
    Task<Result<ShowtimeDto>> GetShowtimeByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ShowtimeDto>> CreateShowtimeAsync(CreateShowtimeDto dto, CancellationToken ct = default);
    Task<Result> DeleteShowtimeAsync(Guid id, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Services/ShowtimeService.cs`

```csharp
using System.Text.Json;
using Application.DTOs.Showtimes;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ShowtimeService : IShowtimeService
{
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ICinemaHallRepository _hallRepository;
    private readonly CinemaDbContext _context;
    private readonly ILogger<ShowtimeService> _logger;
    private readonly TimeProvider _timeProvider;

    public ShowtimeService(
        IShowtimeRepository showtimeRepository,
        IMovieRepository movieRepository,
        ICinemaHallRepository hallRepository,
        CinemaDbContext context,
        ILogger<ShowtimeService> logger,
        TimeProvider? timeProvider = null)
    {
        _showtimeRepository = showtimeRepository;
        _movieRepository = movieRepository;
        _hallRepository = hallRepository;
        _context = context;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<ShowtimeDto>> CreateShowtimeAsync(CreateShowtimeDto dto, CancellationToken ct = default)
    {
        try
        {
            // Validate movie exists
            var movie = await _movieRepository.GetByIdAsync(dto.MovieId, ct);
            if (movie is null)
            {
                return Result<ShowtimeDto>.Failure("Movie not found");
            }

            // Validate cinema hall exists
            var hall = await _hallRepository.GetByIdAsync(dto.CinemaHallId, ct);
            if (hall is null)
            {
                return Result<ShowtimeDto>.Failure("Cinema hall not found");
            }

            // Calculate end time
            var endTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30); // +30 min buffer

            // Check for overlapping showtimes
            var hasOverlap = await _showtimeRepository.HasOverlappingShowtimeAsync(
                dto.CinemaHallId,
                dto.StartTime,
                endTime,
                null,
                ct);

            if (hasOverlap)
            {
                return Result<ShowtimeDto>.Failure("Showtime overlaps with existing showtime in this hall");
            }

            // Create showtime
            var showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                MovieId = dto.MovieId,
                CinemaHallId = dto.CinemaHallId,
                StartTime = dto.StartTime,
                EndTime = endTime,
                BasePrice = dto.BasePrice,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().DateTime
            };

            // Use transaction to create showtime and seats atomically
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var created = await _showtimeRepository.CreateAsync(showtime, ct);

                // Generate seats for this showtime
                await GenerateSeatsForShowtimeAsync(created.Id, hall, dto.BasePrice, ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Showtime created: {ShowtimeId} for movie {MovieId} in hall {HallId}",
                    created.Id,
                    dto.MovieId,
                    dto.CinemaHallId);

                var resultDto = new ShowtimeDto(
                    created.Id,
                    movie.Id,
                    movie.Title,
                    hall.Id,
                    hall.Name,
                    created.StartTime,
                    created.EndTime,
                    created.BasePrice,
                    hall.TotalSeats,
                    created.IsActive
                );

                return Result<ShowtimeDto>.Success(resultDto);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating showtime");
            return Result<ShowtimeDto>.Failure("Failed to create showtime");
        }
    }

    private async Task GenerateSeatsForShowtimeAsync(
        Guid showtimeId,
        CinemaHall hall,
        decimal basePrice,
        CancellationToken ct)
    {
        var seatLayout = JsonSerializer.Deserialize<SeatLayout>(hall.SeatLayoutJson);
        if (seatLayout is null)
        {
            throw new InvalidOperationException("Invalid seat layout JSON");
        }

        var seats = seatLayout.Seats
            .Where(s => s.IsAvailable)
            .Select(s => new Seat
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                SeatNumber = s.SeatNumber,
                SeatType = s.SeatType,
                Price = basePrice * s.PriceMultiplier,
                Status = SeatStatus.Available,
                ReservationId = null,
                ReservedUntil = null,
                RowVersion = Array.Empty<byte>()
            })
            .ToList();

        _context.Seats.AddRange(seats);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Generated {SeatCount} seats for showtime {ShowtimeId}", seats.Count, showtimeId);
    }

    // ... other methods (GetShowtimesAsync, GetShowtimeByIdAsync, etc.)
}
```

---

## Step 7: Create Endpoints

### 7.1 Movie Endpoints

**File:** `Backend/Endpoints/MovieEndpoints.cs`

```csharp
using Application.DTOs.Movies;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class MovieEndpoints
{
    public static RouteGroupBuilder MapMovieEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetAllMoviesAsync)
            .WithName("GetMovies")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetMovieByIdAsync)
            .WithName("GetMovie")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateMovieAsync)
            .WithName("CreateMovie")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:guid}", UpdateMovieAsync)
            .WithName("UpdateMovie")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteMovieAsync)
            .WithName("DeleteMovie")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetAllMoviesAsync(
        IMovieService movieService,
        bool? activeOnly,
        CancellationToken ct)
    {
        var result = await movieService.GetAllMoviesAsync(activeOnly ?? true, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<MovieDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<MovieDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetMovieByIdAsync(
        Guid id,
        IMovieService movieService,
        CancellationToken ct)
    {
        var result = await movieService.GetMovieByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> CreateMovieAsync(
        CreateMovieDto dto,
        IMovieService movieService,
        IValidator<CreateMovieDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<MovieDto>(false, null, "Validation failed", errors));
        }

        var result = await movieService.CreateMovieAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/movies/{result.Value!.Id}", new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateMovieAsync(
        Guid id,
        UpdateMovieDto dto,
        IMovieService movieService,
        CancellationToken ct)
    {
        var result = await movieService.UpdateMovieAsync(id, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteMovieAsync(
        Guid id,
        IMovieService movieService,
        CancellationToken ct)
    {
        var result = await movieService.DeleteMovieAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
```

### 7.2 Showtime Endpoints

**File:** `Backend/Endpoints/ShowtimeEndpoints.cs`

```csharp
using Application.DTOs.Showtimes;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class ShowtimeEndpoints
{
    public static RouteGroupBuilder MapShowtimeEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetShowtimesAsync)
            .WithName("GetShowtimes")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetShowtimeByIdAsync)
            .WithName("GetShowtime")
            .AllowAnonymous();

        group.MapGet("/movie/{movieId:guid}", GetShowtimesByMovieAsync)
            .WithName("GetShowtimesByMovie")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateShowtimeAsync)
            .WithName("CreateShowtime")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteShowtimeAsync)
            .WithName("DeleteShowtime")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetShowtimesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimesAsync(fromDate, toDate, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<ShowtimeDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<ShowtimeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetShowtimeByIdAsync(
        Guid id,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimeByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<ShowtimeDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<ShowtimeDto>(false, null, result.Error));
    }

    private static async Task<IResult> GetShowtimesByMovieAsync(
        Guid movieId,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimesByMovieAsync(movieId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<ShowtimeDto>>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<List<ShowtimeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> CreateShowtimeAsync(
        CreateShowtimeDto dto,
        IShowtimeService showtimeService,
        IValidator<CreateShowtimeDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<ShowtimeDto>(false, null, "Validation failed", errors));
        }

        var result = await showtimeService.CreateShowtimeAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/showtimes/{result.Value!.Id}", new ApiResponse<ShowtimeDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<ShowtimeDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteShowtimeAsync(
        Guid id,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.DeleteShowtimeAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
```

---

## Step 8: Update Program.cs

Add the following to `Backend/Program.cs`:

```csharp
// Repositories
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<ICinemaHallRepository, CinemaHallRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();

// Services
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ... after endpoint mapping ...

app.MapGroup("/api/movies")
    .MapMovieEndpoints()
    .WithTags("Movies");

app.MapGroup("/api/showtimes")
    .MapShowtimeEndpoints()
    .WithTags("Showtimes");
```

---

## Step 9: Create Migration

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend

dotnet ef migrations add AddContentManagement --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
```

---

## Step 10: Testing

### 10.1 Unit Tests for ShowtimeService

**File:** `Tests/Tests.Unit/Services/ShowtimeServiceTests.cs`

```csharp
using Application.DTOs.Showtimes;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Services;

public class ShowtimeServiceTests
{
    [Fact]
    public async Task CreateShowtimeAsync_NoOverlap_ReturnsSuccess()
    {
        // Arrange
        var movieRepoMock = new Mock<IMovieRepository>();
        var hallRepoMock = new Mock<ICinemaHallRepository>();
        var showtimeRepoMock = new Mock<IShowtimeRepository>();
        var contextMock = new Mock<CinemaDbContext>();
        var loggerMock = new Mock<ILogger<ShowtimeService>>();

        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = "Test Movie",
            DurationMinutes = 120,
            IsActive = true
        };

        var hall = new CinemaHall
        {
            Id = Guid.NewGuid(),
            Name = "Hall 1",
            TotalSeats = 100,
            SeatLayoutJson = "{\"Rows\":10,\"SeatsPerRow\":10,\"Seats\":[]}",
            IsActive = true
        };

        movieRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        hallRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hall);

        showtimeRepoMock.Setup(x => x.HasOverlappingShowtimeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ShowtimeService(
            showtimeRepoMock.Object,
            movieRepoMock.Object,
            hallRepoMock.Object,
            contextMock.Object,
            loggerMock.Object);

        var dto = new CreateShowtimeDto(
            movie.Id,
            hall.Id,
            DateTime.UtcNow.AddDays(1),
            10.00m
        );

        // Act
        var result = await service.CreateShowtimeAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateShowtimeAsync_WithOverlap_ReturnsFailure()
    {
        // Similar test for overlapping showtime scenario
        // Assert result.IsSuccess.Should().BeFalse();
        // Assert result.Error.Should().Contain("overlaps");
    }
}
```

---

## Verification Checklist

- [ ] Movie entity created with all fields
- [ ] CinemaHall entity with JSON seat layout
- [ ] Showtime entity with relationships
- [ ] Seat entity with row versioning
- [ ] All repositories implemented
- [ ] MovieService with CRUD operations
- [ ] ShowtimeService with overlap detection
- [ ] Seat generation on showtime creation works
- [ ] Admin endpoints require authentication
- [ ] Public endpoints allow anonymous access
- [ ] Database migration applied successfully
- [ ] Unit tests pass with 80%+ coverage
- [ ] Integration tests for full flow

---

## Common Issues & Solutions

### Issue 1: JSON serialization for SeatLayout
**Solution:** Use `System.Text.Json` for serialization. Ensure proper configuration in DbContext.

### Issue 2: Seats not generated on showtime creation
**Solution:** Verify transaction commits successfully. Check SeatLayoutJson format.

### Issue 3: Overlapping showtime check fails
**Solution:** Ensure datetime comparisons handle timezone correctly (always use UTC).

---

## Next Steps

✅ **Phase 2 Complete!**

Proceed to Phase 3: Seat Reservation System

See: `docs/phases/phase-3-seat-reservation.md`
