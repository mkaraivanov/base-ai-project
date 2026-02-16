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
            // Verify showtime exists
            var showtime = await _showtimeRepository.GetByIdAsync(showtimeId, ct);
            if (showtime is null)
            {
                return Result<SeatAvailabilityDto>.Failure("Showtime not found");
            }

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
        IReadOnlyList<string> seatNumbers,
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
