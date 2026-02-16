# Phase 4: Payment & Booking Confirmation

**Duration:** Week 3
**Status:** 🔵 Pending

## Overview

This phase implements the final booking confirmation flow, connecting reservations to payments and creating confirmed bookings. It includes a mock payment provider for testing, booking number generation, and booking cancellation with seat release.

## Objectives

✅ Implement Payment entity
✅ Implement Booking entity
✅ Create mock payment provider
✅ Build booking confirmation flow (Reservation → Payment → Booking)
✅ Generate unique booking numbers
✅ Implement booking cancellation
✅ Create payment and booking endpoints
✅ Handle payment failures with rollback
✅ Achieve 80%+ test coverage

---

## Step 1: Implement Domain Entities

### 1.1 Payment Entity

**File:** `Backend/Domain/Entities/Payment.cs`

```csharp
namespace Domain.Entities;

public class Payment
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty; // CreditCard, Mock, etc.
    public string TransactionId { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }

    // Navigation
    public Booking? Booking { get; init; }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
```

### 1.2 Booking Entity

**File:** `Backend/Domain/Entities/Booking.cs`

```csharp
namespace Domain.Entities;

public class Booking
{
    public Guid Id { get; init; }
    public string BookingNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid ShowtimeId { get; init; }
    public List<string> SeatNumbers { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public BookingStatus Status { get; init; } = BookingStatus.Confirmed;
    public Guid? PaymentId { get; init; }
    public DateTime BookedAt { get; init; }
    public DateTime? CancelledAt { get; init; }

    // Navigation
    public User? User { get; init; }
    public Showtime? Showtime { get; init; }
    public Payment? Payment { get; init; }
}

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1,
    Refunded = 2
}
```

---

## Step 2: Update DbContext

Add to `Backend/Infrastructure/Data/CinemaDbContext.cs`:

```csharp
public DbSet<Payment> Payments => Set<Payment>();
public DbSet<Booking> Bookings => Set<Booking>();

// In OnModelCreating:
modelBuilder.Entity<Payment>(entity =>
{
    entity.HasKey(e => e.Id);

    entity.Property(e => e.Amount)
        .HasPrecision(10, 2);

    entity.Property(e => e.PaymentMethod)
        .IsRequired()
        .HasMaxLength(50);

    entity.Property(e => e.TransactionId)
        .IsRequired()
        .HasMaxLength(100);

    entity.HasOne(e => e.Booking)
        .WithOne(b => b.Payment)
        .HasForeignKey<Payment>(e => e.BookingId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(e => e.TransactionId).IsUnique();
    entity.HasIndex(e => e.Status);
});

modelBuilder.Entity<Booking>(entity =>
{
    entity.HasKey(e => e.Id);

    entity.Property(e => e.BookingNumber)
        .IsRequired()
        .HasMaxLength(20);

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

    entity.HasIndex(e => e.BookingNumber).IsUnique();
    entity.HasIndex(e => new { e.UserId, e.Status });
    entity.HasIndex(e => e.ShowtimeId);
});
```

---

## Step 3: Create DTOs

**File:** `Backend/Application/DTOs/Payments/ProcessPaymentDto.cs`

```csharp
namespace Application.DTOs.Payments;

public record ProcessPaymentDto(
    Guid ReservationId,
    string PaymentMethod,
    string CardNumber,
    string CardHolderName,
    string ExpiryDate,
    string CVV
);
```

**File:** `Backend/Application/DTOs/Payments/PaymentResultDto.cs`

```csharp
namespace Application.DTOs.Payments;

public record PaymentResultDto(
    Guid PaymentId,
    string TransactionId,
    string Status,
    decimal Amount,
    DateTime ProcessedAt
);
```

**File:** `Backend/Application/DTOs/Bookings/BookingDto.cs`

```csharp
namespace Application.DTOs.Bookings;

public record BookingDto(
    Guid Id,
    string BookingNumber,
    Guid ShowtimeId,
    string MovieTitle,
    DateTime ShowtimeStart,
    string HallName,
    List<string> SeatNumbers,
    decimal TotalAmount,
    string Status,
    DateTime BookedAt
);
```

**File:** `Backend/Application/DTOs/Bookings/ConfirmBookingDto.cs`

```csharp
namespace Application.DTOs.Bookings;

public record ConfirmBookingDto(
    Guid ReservationId,
    Guid PaymentId
);
```

---

## Step 4: Create Booking Number Generator

**File:** `Backend/Application/Helpers/BookingNumberGenerator.cs`

```csharp
namespace Application.Helpers;

public static class BookingNumberGenerator
{
    private static readonly Random _random = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Generates a unique booking number in format: BK-YYMMDD-XXXXX
    /// Example: BK-240115-A1B2C
    /// </summary>
    public static string Generate()
    {
        lock (_lock)
        {
            var date = DateTime.UtcNow.ToString("yyMMdd");
            var randomPart = GenerateRandomAlphanumeric(5);
            return $"BK-{date}-{randomPart}";
        }
    }

    private static string GenerateRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar-looking characters
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }

        return new string(result);
    }
}
```

---

## Step 5: Create Mock Payment Service

**File:** `Backend/Application/Services/IPaymentService.cs`

```csharp
using Application.DTOs.Payments;
using Domain.Common;

namespace Application.Services;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(ProcessPaymentDto dto, decimal amount, CancellationToken ct = default);
    Task<Result<PaymentResultDto>> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Services/MockPaymentService.cs`

```csharp
using Application.DTOs.Payments;
using Application.Services;
using Domain.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly TimeProvider _timeProvider;

    public MockPaymentService(
        ILogger<MockPaymentService> logger,
        TimeProvider? timeProvider = null)
    {
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<PaymentResultDto>> ProcessPaymentAsync(
        ProcessPaymentDto dto,
        decimal amount,
        CancellationToken ct = default)
    {
        try
        {
            // Simulate payment processing delay
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            // Mock validation - fail if card number starts with "0000"
            if (dto.CardNumber.StartsWith("0000"))
            {
                _logger.LogWarning("Mock payment failed for reservation {ReservationId}", dto.ReservationId);
                return Result<PaymentResultDto>.Failure("Payment declined by bank");
            }

            var transactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var result = new PaymentResultDto(
                Guid.NewGuid(),
                transactionId,
                "Completed",
                amount,
                _timeProvider.GetUtcNow().DateTime
            );

            _logger.LogInformation(
                "Mock payment processed successfully: {TransactionId}, Amount: {Amount}",
                transactionId,
                amount);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mock payment");
            return Result<PaymentResultDto>.Failure("Payment processing failed");
        }
    }

    public async Task<Result<PaymentResultDto>> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            // Simulate refund processing
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            var transactionId = $"RFN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var result = new PaymentResultDto(
                paymentId,
                transactionId,
                "Refunded",
                0, // Amount will be set by caller
                _timeProvider.GetUtcNow().DateTime
            );

            _logger.LogInformation("Mock refund processed: {TransactionId}", transactionId);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return Result<PaymentResultDto>.Failure("Refund processing failed");
        }
    }
}
```

---

## Step 6: Create Repositories

### 6.1 Payment Repository

**File:** `Backend/Infrastructure/Repositories/IPaymentRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/PaymentRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly CinemaDbContext _context;

    public PaymentRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }
}
```

### 6.2 Booking Repository

**File:** `Backend/Infrastructure/Repositories/IBookingRepository.cs`

```csharp
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct = default);
    Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default);
    Task<Booking> UpdateAsync(Booking booking, CancellationToken ct = default);
}
```

**File:** `Backend/Infrastructure/Repositories/BookingRepository.cs`

```csharp
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly CinemaDbContext _context;

    public BookingRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, ct);
    }

    public async Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(ct);
    }

    public async Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync(ct);
        return booking;
    }
}
```

---

## Step 7: Update Booking Service

Add the following methods to `Backend/Infrastructure/Services/BookingService.cs`:

```csharp
private readonly IPaymentService _paymentService;
private readonly IPaymentRepository _paymentRepository;
private readonly IBookingRepository _bookingRepository;

// Add to constructor parameters

public async Task<Result<BookingDto>> ConfirmBookingAsync(
    Guid userId,
    Guid reservationId,
    ProcessPaymentDto paymentDto,
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
            return Result<BookingDto>.Failure("Reservation not found");
        }

        // Verify ownership
        if (reservation.UserId != userId)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Unauthorized");
        }

        // Check if reservation is still valid
        if (reservation.Status != ReservationStatus.Pending)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Reservation is no longer valid");
        }

        if (reservation.ExpiresAt < _timeProvider.GetUtcNow().DateTime)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Reservation has expired");
        }

        // Process payment
        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentDto, reservation.TotalAmount, ct);
        if (!paymentResult.IsSuccess)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure($"Payment failed: {paymentResult.Error}");
        }

        // Create payment record
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.Empty, // Will be set after booking creation
            Amount = reservation.TotalAmount,
            PaymentMethod = paymentDto.PaymentMethod,
            TransactionId = paymentResult.Value!.TransactionId,
            Status = PaymentStatus.Completed,
            CreatedAt = _timeProvider.GetUtcNow().DateTime,
            ProcessedAt = paymentResult.Value.ProcessedAt
        };

        // Create booking
        var bookingNumber = BookingNumberGenerator.Generate();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingNumber = bookingNumber,
            UserId = userId,
            ShowtimeId = reservation.ShowtimeId,
            SeatNumbers = reservation.SeatNumbers,
            TotalAmount = reservation.TotalAmount,
            Status = BookingStatus.Confirmed,
            PaymentId = payment.Id,
            BookedAt = _timeProvider.GetUtcNow().DateTime,
            CancelledAt = null
        };

        payment = payment with { BookingId = booking.Id };

        await _paymentRepository.CreateAsync(payment, ct);
        await _bookingRepository.CreateAsync(booking, ct);

        // Update seat status to Booked
        var seats = await _seatRepository.GetByReservationIdAsync(reservationId, ct);
        var bookedSeats = seats.Select(s => s with
        {
            Status = SeatStatus.Booked,
            ReservationId = null,
            ReservedUntil = null
        }).ToList();

        await _seatRepository.UpdateRangeAsync(bookedSeats, ct);

        // Mark reservation as confirmed
        var confirmedReservation = reservation with { Status = ReservationStatus.Confirmed };
        await _reservationRepository.UpdateAsync(confirmedReservation, ct);

        await _unitOfWork.CommitTransactionAsync(ct);

        _logger.LogInformation(
            "Booking confirmed: {BookingNumber} for user {UserId}",
            bookingNumber,
            userId);

        var showtime = await _showtimeRepository.GetByIdAsync(booking.ShowtimeId, ct);
        var resultDto = new BookingDto(
            booking.Id,
            booking.BookingNumber,
            booking.ShowtimeId,
            showtime!.Movie!.Title,
            showtime.StartTime,
            showtime.CinemaHall!.Name,
            booking.SeatNumbers,
            booking.TotalAmount,
            booking.Status.ToString(),
            booking.BookedAt
        );

        return Result<BookingDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        _logger.LogError(ex, "Error confirming booking for reservation {ReservationId}", reservationId);
        return Result<BookingDto>.Failure("Failed to confirm booking");
    }
}

public async Task<Result<BookingDto>> CancelBookingAsync(
    Guid userId,
    Guid bookingId,
    CancellationToken ct = default)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync(ct);

        // Get booking
        var booking = await _bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking is null)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Booking not found");
        }

        // Verify ownership
        if (booking.UserId != userId)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Unauthorized");
        }

        // Check if already cancelled
        if (booking.Status != BookingStatus.Confirmed)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Booking cannot be cancelled");
        }

        // Check if showtime is in the future
        var showtime = await _showtimeRepository.GetByIdAsync(booking.ShowtimeId, ct);
        if (showtime is null || showtime.StartTime <= _timeProvider.GetUtcNow().DateTime)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<BookingDto>.Failure("Cannot cancel past or ongoing showtimes");
        }

        // Process refund
        if (booking.PaymentId.HasValue)
        {
            var refundResult = await _paymentService.RefundPaymentAsync(booking.PaymentId.Value, ct);
            if (!refundResult.IsSuccess)
            {
                _logger.LogWarning("Refund failed for booking {BookingId}, proceeding with cancellation", bookingId);
            }
        }

        // Release seats
        var seats = await _seatRepository.GetByShowtimeAndNumbersAsync(booking.ShowtimeId, booking.SeatNumbers, ct);
        var releasedSeats = seats.Select(s => s with
        {
            Status = SeatStatus.Available,
            ReservationId = null,
            ReservedUntil = null
        }).ToList();

        await _seatRepository.UpdateRangeAsync(releasedSeats, ct);

        // Update booking status
        var cancelledBooking = booking with
        {
            Status = BookingStatus.Cancelled,
            CancelledAt = _timeProvider.GetUtcNow().DateTime
        };

        await _bookingRepository.UpdateAsync(cancelledBooking, ct);

        await _unitOfWork.CommitTransactionAsync(ct);

        _logger.LogInformation("Booking cancelled: {BookingNumber}", booking.BookingNumber);

        var resultDto = new BookingDto(
            cancelledBooking.Id,
            cancelledBooking.BookingNumber,
            cancelledBooking.ShowtimeId,
            showtime!.Movie!.Title,
            showtime.StartTime,
            showtime.CinemaHall!.Name,
            cancelledBooking.SeatNumbers,
            cancelledBooking.TotalAmount,
            cancelledBooking.Status.ToString(),
            cancelledBooking.BookedAt
        );

        return Result<BookingDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
        return Result<BookingDto>.Failure("Failed to cancel booking");
    }
}
```

---

## Step 8: Create Endpoints

Update `Backend/Endpoints/BookingEndpoints.cs`:

```csharp
group.MapPost("/confirm", ConfirmBookingAsync)
    .WithName("ConfirmBooking")
    .RequireAuthorization();

group.MapGet("/my-bookings", GetMyBookingsAsync)
    .WithName("GetMyBookings")
    .RequireAuthorization();

group.MapGet("/{bookingNumber}", GetBookingByNumberAsync)
    .WithName("GetBookingByNumber")
    .RequireAuthorization();

group.MapPost("/{bookingId:guid}/cancel", CancelBookingAsync)
    .WithName("CancelBooking")
    .RequireAuthorization();

private static async Task<IResult> ConfirmBookingAsync(
    ConfirmBookingDto dto,
    IBookingService bookingService,
    HttpContext context,
    CancellationToken ct)
{
    // Extract userId from claims
    var userId = GetUserIdFromClaims(context);
    if (userId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    var result = await bookingService.ConfirmBookingAsync(userId, dto.ReservationId, /* payment dto */, ct);

    return result.IsSuccess
        ? Results.Ok(new ApiResponse<BookingDto>(true, result.Value, null))
        : Results.BadRequest(new ApiResponse<BookingDto>(false, null, result.Error));
}
```

---

## Step 9: Create Migration

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project/Backend

dotnet ef migrations add AddPaymentAndBooking --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
```

---

## Verification Checklist

- [ ] Payment and Booking entities created
- [ ] Mock payment service implemented
- [ ] Booking number generator works
- [ ] Confirm booking flow completes successfully
- [ ] Payment failure rolls back correctly
- [ ] Booking cancellation releases seats
- [ ] Refund processed on cancellation
- [ ] Database migration applied
- [ ] 80%+ test coverage

---

## Next Steps

✅ **Phase 4 Complete!**

Proceed to Phase 5: Frontend Development

See: `docs/phases/phase-5-frontend.md`
