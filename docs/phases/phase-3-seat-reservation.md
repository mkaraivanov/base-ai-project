# Phase 3: Seat Reservation System

**Duration:** Week 2-3
**Status:** 🔵 Pending
**Complexity:** 🔴 HIGH (Critical Concurrency Handling)

## Overview

This is the **most critical phase** of the cinema booking system. It implements the seat reservation mechanism with optimistic concurrency control to prevent double-booking. The system allows users to temporarily reserve seats (5-minute hold), handles concurrent booking attempts, and automatically releases expired reservations through a background service.

## Objectives

✅ Implement Reservation entity
✅ Build seat availability queries (optimized for performance)
✅ Create reservation system with optimistic concurrency
✅ Handle DbUpdateConcurrencyException gracefully
✅ Implement Unit of Work pattern for transactions
✅ Create background service for expired reservation cleanup
✅ Build booking endpoints (availability, reserve, cancel)
✅ Test concurrent booking scenarios
✅ Achieve 80%+ test coverage with focus on edge cases

---

## Step 1: Implement Reservation Entity

**File:** `Backend/Domain/Entities/Reservation.cs`

```csharp
namespace Domain.Entities;

public class Reservation
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ShowtimeId { get; init; }
    public List<string> SeatNumbers { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public DateTime ExpiresAt { get; init; }
    public ReservationStatus Status { get; init; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; init; }

    // Navigation properties
    public User? User { get; init; }
    public Showtime? Showtime { get; init; }
}

public enum ReservationStatus
{
    Pending = 0,
    Expired = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

---

## Step 2: Update DbContext

Add to `Backend/Infrastructure/Data/CinemaDbContext.cs`:

```csharp
public DbSet<Reservation> Reservations => Set<Reservation>();

// In OnModelCreating:
modelBuilder.Entity<Reservation>(entity =>
{
    entity.HasKey(e => e.Id);

    entity.Property(e => e.TotalAmount)
        .HasPrecision(10, 2);

    entity.Property(e => e.SeatNumbers)
        .HasConversion(
            v => string.Join(',', v),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        )
        .HasMaxLength(500);

    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.Showtime)
        .WithMany()
        .HasForeignKey(e => e.ShowtimeId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(e => new { e.UserId, e.Status });
    entity.HasIndex(e => e.ExpiresAt); // Critical for cleanup job
    entity.HasIndex(e => new { e.ShowtimeId, e.Status });
});
```

---

## Step 3: Implement Unit of Work Pattern

**File:** `Backend/Infrastructure/UnitOfWork/IUnitOfWork.cs`

```csharp
namespace Infrastructure.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/UnitOfWork/UnitOfWork.cs`

```csharp
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly CinemaDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction");
        }

        try
        {
            await _context.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
```

---

## Step 4: Create DTOs

**File:** `Backend/Application/DTOs/Reservations/CreateReservationDto.cs`

```csharp
namespace Application.DTOs.Reservations;

public record CreateReservationDto(
    Guid ShowtimeId,
    List<string> SeatNumbers
);
```

**File:** `Backend/Application/DTOs/Reservations/ReservationDto.cs`

```csharp
namespace Application.DTOs.Reservations;

public record ReservationDto(
    Guid Id,
    Guid ShowtimeId,
    List<string> SeatNumbers,
    decimal TotalAmount,
    DateTime ExpiresAt,
    string Status,
    DateTime CreatedAt
);
```

**File:** `Backend/Application/DTOs/Seats/SeatAvailabilityDto.cs`

```csharp
namespace Application.DTOs.Seats;

public record SeatAvailabilityDto(
    Guid ShowtimeId,
    List<SeatDto> AvailableSeats,
    List<SeatDto> ReservedSeats,
    List<SeatDto> BookedSeats,
    int TotalSeats
);

public record SeatDto(
    string SeatNumber,
    string SeatType,
    decimal Price,
    string Status
);
```

---

## Step 5: Create Validators

**File:** `Backend/Application/Validators/CreateReservationDtoValidator.cs`

```csharp
using Application.DTOs.Reservations;
using FluentValidation;

namespace Application.Validators;

public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .NotEmpty().WithMessage("Showtime ID is required");

        RuleFor(x => x.SeatNumbers)
            .NotEmpty().WithMessage("At least one seat must be selected")
            .Must(seats => seats.Count <= 10)
            .WithMessage("Cannot reserve more than 10 seats at once")
            .Must(seats => seats.Distinct().Count() == seats.Count)
            .WithMessage("Duplicate seat numbers are not allowed");

        RuleForEach(x => x.SeatNumbers)
            .NotEmpty().WithMessage("Seat number cannot be empty")
            .Matches(@"^[A-Z]\d{1,2}$").WithMessage("Invalid seat number format (e.g., A1, B12)");
    }
}
```

---

## Step 6: Create Repositories

### 6.1 Seat Repository

**File:** `Backend/Infrastructure/Repositories/ISeatRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByShowtimeIdAsync(Guid showtimeId, CancellationToken ct = default);
    Task<List<Seat>> GetByShowtimeAndNumbersAsync(Guid showtimeId, List<string> seatNumbers, CancellationToken ct = default);
    Task<List<Seat>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default);
    Task UpdateAsync(Seat seat, CancellationToken ct = default);
    Task UpdateRangeAsync(List<Seat> seats, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/SeatRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly CinemaDbContext _context;

    public SeatRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetByShowtimeIdAsync(Guid showtimeId, CancellationToken ct = default)
    {
        return await _context.Seats
            .AsNoTracking()
            .Where(s => s.ShowtimeId == showtimeId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetByShowtimeAndNumbersAsync(
        Guid showtimeId,
        List<string> seatNumbers,
        CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => s.ShowtimeId == showtimeId && seatNumbers.Contains(s.SeatNumber))
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => s.ReservationId == reservationId)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Seat seat, CancellationToken ct = default)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(List<Seat> seats, CancellationToken ct = default)
    {
        _context.Seats.UpdateRange(seats);
        await _context.SaveChangesAsync(ct);
    }
}
```

### 6.2 Reservation Repository

**File:** `Backend/Infrastructure/Repositories/IReservationRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Reservation>> GetExpiredReservationsAsync(DateTime currentTime, CancellationToken ct = default);
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default);
    Task<Reservation> UpdateAsync(Reservation reservation, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/ReservationRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly CinemaDbContext _context;

    public ReservationRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Showtime)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<List<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Showtime)
                .ThenInclude(s => s!.Movie)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Reservation>> GetExpiredReservationsAsync(DateTime currentTime, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt < currentTime)
            .ToListAsync(ct);
    }

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync(ct);
        return reservation;
    }
}
```

---

## Step 7: Create Booking Service

**File:** `Backend/Application/Services/IBookingService.cs`

```csharp
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Domain.Common;

namespace Application.Services;

public interface IBookingService
{
    Task<Result<SeatAvailabilityDto>> GetSeatAvailabilityAsync(Guid showtimeId, CancellationToken ct = default);
    Task<Result<ReservationDto>> CreateReservationAsync(Guid userId, CreateReservationDto dto, CancellationToken ct = default);
    Task<Result> CancelReservationAsync(Guid userId, Guid reservationId, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Services/BookingService.cs`

```csharp
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;
    private readonly TimeProvider _timeProvider;

    public BookingService(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IShowtimeRepository showtimeRepository,
        IUnitOfWork unitOfWork,
        ILogger<BookingService> logger,
        TimeProvider? timeProvider = null)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _showtimeRepository = showtimeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<SeatAvailabilityDto>> GetSeatAvailabilityAsync(
        Guid showtimeId,
        CancellationToken ct = default)
    {
        try
        {
            var seats = await _seatRepository.GetByShowtimeIdAsync(showtimeId, ct);

            var availableSeats = seats
                .Where(s => s.Status == SeatStatus.Available)
                .Select(s => new SeatDto(s.SeatNumber, s.SeatType, s.Price, "Available"))
                .ToList();

            var reservedSeats = seats
                .Where(s => s.Status == SeatStatus.Reserved)
                .Select(s => new SeatDto(s.SeatNumber, s.SeatType, s.Price, "Reserved"))
                .ToList();

            var bookedSeats = seats
                .Where(s => s.Status == SeatStatus.Booked)
                .Select(s => new SeatDto(s.SeatNumber, s.SeatType, s.Price, "Booked"))
                .ToList();

            var result = new SeatAvailabilityDto(
                showtimeId,
                availableSeats,
                reservedSeats,
                bookedSeats,
                seats.Count
            );

            return Result<SeatAvailabilityDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat availability for showtime {ShowtimeId}", showtimeId);
            return Result<SeatAvailabilityDto>.Failure("Failed to retrieve seat availability");
        }
    }

    public async Task<Result<ReservationDto>> CreateReservationAsync(
        Guid userId,
        CreateReservationDto dto,
        CancellationToken ct = default)
    {
        try
        {
            // Start transaction
            await _unitOfWork.BeginTransactionAsync(ct);

            // Validate showtime exists and is in the future
            var showtime = await _showtimeRepository.GetByIdAsync(dto.ShowtimeId, ct);
            if (showtime is null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<ReservationDto>.Failure("Showtime not found");
            }

            if (showtime.StartTime <= _timeProvider.GetUtcNow().DateTime)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<ReservationDto>.Failure("Cannot book past or ongoing showtimes");
            }

            // Attempt to reserve seats (optimistic concurrency will catch conflicts)
            var reserveResult = await ReserveSeatsAsync(dto.ShowtimeId, dto.SeatNumbers, ct);

            if (!reserveResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<ReservationDto>.Failure(reserveResult.Error!);
            }

            var reservedSeats = reserveResult.Value!;

            // Calculate total amount
            var totalAmount = reservedSeats.Sum(s => s.Price);

            // Create reservation
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                Id = reservationId,
                UserId = userId,
                ShowtimeId = dto.ShowtimeId,
                SeatNumbers = dto.SeatNumbers,
                TotalAmount = totalAmount,
                ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(5).DateTime,
                Status = ReservationStatus.Pending,
                CreatedAt = _timeProvider.GetUtcNow().DateTime
            };

            // Update seat reservation IDs
            var updatedSeats = reservedSeats.Select(s => s with { ReservationId = reservationId }).ToList();
            await _seatRepository.UpdateRangeAsync(updatedSeats, ct);

            await _reservationRepository.CreateAsync(reservation, ct);

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Reservation created: {ReservationId} for user {UserId}, seats: {SeatNumbers}",
                reservationId,
                userId,
                string.Join(", ", dto.SeatNumbers));

            var resultDto = new ReservationDto(
                reservation.Id,
                reservation.ShowtimeId,
                reservation.SeatNumbers,
                reservation.TotalAmount,
                reservation.ExpiresAt,
                reservation.Status.ToString(),
                reservation.CreatedAt
            );

            return Result<ReservationDto>.Success(resultDto);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogWarning(ex, "Concurrency conflict during reservation for user {UserId}", userId);
            return Result<ReservationDto>.Failure("Selected seats are no longer available. Please try again.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Error creating reservation for user {UserId}", userId);
            return Result<ReservationDto>.Failure("Failed to create reservation");
        }
    }

    private async Task<Result<List<Seat>>> ReserveSeatsAsync(
        Guid showtimeId,
        List<string> seatNumbers,
        CancellationToken ct)
    {
        var seats = await _seatRepository.GetByShowtimeAndNumbersAsync(showtimeId, seatNumbers, ct);

        if (seats.Count != seatNumbers.Count)
        {
            var foundSeatNumbers = seats.Select(s => s.SeatNumber).ToHashSet();
            var missingSeatNumbers = seatNumbers.Except(foundSeatNumbers).ToList();
            return Result<List<Seat>>.Failure($"Seats not found: {string.Join(", ", missingSeatNumbers)}");
        }

        // Check all seats are available
        var unavailableSeats = seats.Where(s => s.Status != SeatStatus.Available).ToList();
        if (unavailableSeats.Any())
        {
            return Result<List<Seat>>.Failure(
                $"Seats not available: {string.Join(", ", unavailableSeats.Select(s => s.SeatNumber))}");
        }

        // Reserve seats (optimistic concurrency will catch conflicts on update)
        var reservedSeats = seats.Select(s => s with
        {
            Status = SeatStatus.Reserved,
            ReservedUntil = _timeProvider.GetUtcNow().AddMinutes(5).DateTime
        }).ToList();

        await _seatRepository.UpdateRangeAsync(reservedSeats, ct);

        return Result<List<Seat>>.Success(reservedSeats);
    }

    public async Task<Result> CancelReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken ct = default)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            // Get reservation
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, ct);
            if (reservation is null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result.Failure("Reservation not found");
            }

            // Verify ownership
            if (reservation.UserId != userId)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result.Failure("Unauthorized");
            }

            // Check if already cancelled or expired
            if (reservation.Status != ReservationStatus.Pending)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result.Failure("Reservation cannot be cancelled");
            }

            // Release seats
            var seats = await _seatRepository.GetByReservationIdAsync(reservationId, ct);
            var releasedSeats = seats.Select(s => s with
            {
                Status = SeatStatus.Available,
                ReservationId = null,
                ReservedUntil = null
            }).ToList();

            await _seatRepository.UpdateRangeAsync(releasedSeats, ct);

            // Update reservation status
            var cancelledReservation = reservation with { Status = ReservationStatus.Cancelled };
            await _reservationRepository.UpdateAsync(cancelledReservation, ct);

            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Reservation cancelled: {ReservationId}", reservationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", reservationId);
            return Result.Failure("Failed to cancel reservation");
        }
    }
}
```

---

## Step 8: Create Background Service

**File:** `Backend/Infrastructure/BackgroundServices/ExpiredReservationCleanupService.cs`

```csharp
using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredReservationCleanupService> _logger;
    private readonly TimeProvider _timeProvider;

    public ExpiredReservationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredReservationCleanupService> logger,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired reservation cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);

                // Run every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expired reservation cleanup service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Expired reservation cleanup service stopped");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationRepo = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var seatRepo = scope.ServiceProvider.GetRequiredService<ISeatRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var currentTime = _timeProvider.GetUtcNow().DateTime;
        var expiredReservations = await reservationRepo.GetExpiredReservationsAsync(currentTime, ct);

        if (!expiredReservations.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} expired reservations", expiredReservations.Count);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                await ReleaseReservationSeatsAsync(reservation, seatRepo, reservationRepo, unitOfWork, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired reservation {ReservationId}", reservation.Id);
            }
        }
    }

    private async Task ReleaseReservationSeatsAsync(
        Reservation reservation,
        ISeatRepository seatRepo,
        IReservationRepository reservationRepo,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            // Release seats
            var seats = await seatRepo.GetByReservationIdAsync(reservation.Id, ct);
            var releasedSeats = seats.Select(s => s with
            {
                Status = SeatStatus.Available,
                ReservationId = null,
                ReservedUntil = null
            }).ToList();

            await seatRepo.UpdateRangeAsync(releasedSeats, ct);

            // Mark reservation as expired
            var expiredReservation = reservation with { Status = ReservationStatus.Expired };
            await reservationRepo.UpdateAsync(expiredReservation, ct);

            await unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Released {SeatCount} seats for expired reservation {ReservationId}",
                seats.Count,
                reservation.Id);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Failed to release seats for reservation {ReservationId}", reservation.Id);
            throw;
        }
    }
}
```

---

## Step 9: Create Endpoints

**File:** `Backend/Endpoints/BookingEndpoints.cs`

```csharp
using System.Security.Claims;
using Application.DTOs.Reservations;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoint
        group.MapGet("/availability/{showtimeId:guid}", GetSeatAvailabilityAsync)
            .WithName("GetSeatAvailability")
            .AllowAnonymous();

        // Authenticated endpoints
        group.MapPost("/reserve", CreateReservationAsync)
            .WithName("CreateReservation")
            .RequireAuthorization();

        group.MapDelete("/reserve/{reservationId:guid}", CancelReservationAsync)
            .WithName("CancelReservation")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetSeatAvailabilityAsync(
        Guid showtimeId,
        IBookingService bookingService,
        CancellationToken ct)
    {
        var result = await bookingService.GetSeatAvailabilityAsync(showtimeId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<SeatAvailabilityDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<SeatAvailabilityDto>(false, null, result.Error));
    }

    private static async Task<IResult> CreateReservationAsync(
        CreateReservationDto dto,
        IBookingService bookingService,
        IValidator<CreateReservationDto> validator,
        HttpContext context,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<ReservationDto>(false, null, "Validation failed", errors));
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        // Create reservation
        var result = await bookingService.CreateReservationAsync(userId, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<ReservationDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<ReservationDto>(false, null, result.Error));
    }

    private static async Task<IResult> CancelReservationAsync(
        Guid reservationId,
        IBookingService bookingService,
        HttpContext context,
        CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.CancelReservationAsync(userId, reservationId, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
```

---

## Step 10: Update Program.cs

Add to `Backend/Program.cs`:

```csharp
// Repositories
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

// Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Background Services
builder.Services.AddHostedService<ExpiredReservationCleanupService>();

// ... endpoint mapping ...
app.MapGroup("/api/bookings")
    .MapBookingEndpoints()
    .WithTags("Bookings");
```

---

## Step 11: Create Migration

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend

dotnet ef migrations add AddReservationSystem --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
```

---

## Step 12: Testing (CRITICAL)

### 12.1 Concurrent Booking Tests

**File:** `Tests/Tests.Integration/ConcurrentBookingTests.cs`

```csharp
using System.Net.Http.Json;
using Application.DTOs.Reservations;
using FluentAssertions;
using Tests.Integration.Helpers;

namespace Tests.Integration;

public class ConcurrentBookingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConcurrentBookingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReservation_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1, token2) = await TestDataHelper.SetupConcurrentBookingScenarioAsync(client);

        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });

        // Act - Simulate concurrent requests
        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new("Bearer", token1);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new("Bearer", token2);

        var task1 = client1.PostAsJsonAsync("/api/bookings/reserve", dto);
        var task2 = client2.PostAsJsonAsync("/api/bookings/reserve", dto);

        var responses = await Task.WhenAll(task1, task2);

        // Assert
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failureCount = responses.Count(r => !r.IsSuccessStatusCode);

        successCount.Should().Be(1, "only one user should successfully reserve the seats");
        failureCount.Should().Be(1, "the other user should fail due to concurrency");
    }

    [Fact]
    public async Task CreateReservation_AfterExpiration_SeatsAvailableAgain()
    {
        // Test that expired reservations are cleaned up and seats become available
        // This requires manipulating time or waiting for expiration
    }
}
```

---

## Verification Checklist

- [ ] Reservation entity with all fields
- [ ] Unit of Work pattern implemented
- [ ] Seat repository with optimistic concurrency
- [ ] Reservation repository with expiration query
- [ ] BookingService with transaction management
- [ ] Background cleanup service running
- [ ] Concurrent booking test passes
- [ ] Expiration test passes
- [ ] DbUpdateConcurrencyException handled gracefully
- [ ] Database migration applied
- [ ] 80%+ test coverage on critical paths

---

## Common Issues & Solutions

### Issue 1: DbUpdateConcurrencyException not caught
**Solution:** Ensure `RowVersion` property is properly configured with `IsRowVersion()` in DbContext.

### Issue 2: Background service not running
**Solution:** Verify `AddHostedService` is called in Program.cs. Check logs for startup errors.

### Issue 3: Seats not released after expiration
**Solution:** Check that `ExpiresAt` index exists. Verify background service is querying correctly.

### Issue 4: Transaction deadlocks
**Solution:** Keep transactions short. Ensure proper ordering of entity updates.

---

## Next Steps

✅ **Phase 3 Complete!**

Proceed to Phase 4: Payment & Booking Confirmation

See: `docs/phases/phase-4-payment-booking.md`
