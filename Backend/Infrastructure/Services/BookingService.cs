using Application.DTOs.Bookings;
using Application.DTOs.Payments;
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Application.Helpers;
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
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;
    private readonly TimeProvider _timeProvider;

    public BookingService(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IShowtimeRepository showtimeRepository,
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        ILogger<BookingService> logger,
        TimeProvider? timeProvider = null)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _showtimeRepository = showtimeRepository;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
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

            if (showtime.StartTime <= _timeProvider.GetUtcNow().UtcDateTime)
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
                ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(5).UtcDateTime,
                Status = ReservationStatus.Pending,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
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
            ReservedUntil = _timeProvider.GetUtcNow().AddMinutes(5).UtcDateTime
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

    public async Task<Result<BookingDto>> ConfirmBookingAsync(
        Guid userId,
        ConfirmBookingDto dto,
        CancellationToken ct = default)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            // Get reservation
            var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, ct);
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

            if (reservation.ExpiresAt < _timeProvider.GetUtcNow().UtcDateTime)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<BookingDto>.Failure("Reservation has expired");
            }

            // Process payment
            var paymentDto = new ProcessPaymentDto(
                dto.ReservationId,
                dto.PaymentMethod,
                dto.CardNumber,
                dto.CardHolderName,
                dto.ExpiryDate,
                dto.CVV
            );

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentDto, reservation.TotalAmount, ct);
            if (!paymentResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<BookingDto>.Failure($"Payment failed: {paymentResult.Error}");
            }

            // Create booking
            var bookingNumber = BookingNumberGenerator.Generate();
            var bookingId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            // Create payment record
            var payment = new Payment
            {
                Id = paymentId,
                BookingId = bookingId,
                Amount = reservation.TotalAmount,
                PaymentMethod = dto.PaymentMethod,
                TransactionId = paymentResult.Value!.TransactionId,
                Status = PaymentStatus.Completed,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                ProcessedAt = paymentResult.Value.ProcessedAt
            };

            // Normalize license plate: uppercase and trimmed
            var normalizedPlate = string.IsNullOrWhiteSpace(dto.CarLicensePlate)
                ? null
                : dto.CarLicensePlate.Trim().ToUpperInvariant();

            var booking = new Booking
            {
                Id = bookingId,
                BookingNumber = bookingNumber,
                UserId = userId,
                ShowtimeId = reservation.ShowtimeId,
                SeatNumbers = reservation.SeatNumbers.ToList(),
                TotalAmount = reservation.TotalAmount,
                Status = BookingStatus.Confirmed,
                PaymentId = paymentId,
                BookedAt = _timeProvider.GetUtcNow().UtcDateTime,
                CancelledAt = null,
                CarLicensePlate = normalizedPlate
            };

            await _bookingRepository.CreateAsync(booking, ct);
            await _paymentRepository.CreateAsync(payment, ct);

            // Update seat status to Booked
            var seats = await _seatRepository.GetByReservationIdAsync(dto.ReservationId, ct);
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
                booking.BookedAt,
                booking.CarLicensePlate
            );

            return Result<BookingDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Error confirming booking for reservation {ReservationId}", dto.ReservationId);
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
            if (showtime is null || showtime.StartTime <= _timeProvider.GetUtcNow().UtcDateTime)
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
            var cancelledBooking = new Booking
            {
                Id = booking.Id,
                BookingNumber = booking.BookingNumber,
                UserId = booking.UserId,
                ShowtimeId = booking.ShowtimeId,
                SeatNumbers = booking.SeatNumbers,
                TotalAmount = booking.TotalAmount,
                Status = BookingStatus.Cancelled,
                PaymentId = booking.PaymentId,
                BookedAt = booking.BookedAt,
                CancelledAt = _timeProvider.GetUtcNow().UtcDateTime,
                CarLicensePlate = booking.CarLicensePlate
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
                cancelledBooking.BookedAt,
                cancelledBooking.CarLicensePlate
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

    public async Task<Result<List<BookingDto>>> GetMyBookingsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var bookings = await _bookingRepository.GetByUserIdAsync(userId, ct);

            var bookingDtos = bookings.Select(b => new BookingDto(
                b.Id,
                b.BookingNumber,
                b.ShowtimeId,
                b.Showtime!.Movie!.Title,
                b.Showtime.StartTime,
                b.Showtime.CinemaHall!.Name,
                b.SeatNumbers,
                b.TotalAmount,
                b.Status.ToString(),
                b.BookedAt,
                b.CarLicensePlate
            )).ToList();

            return Result<List<BookingDto>>.Success(bookingDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for user {UserId}", userId);
            return Result<List<BookingDto>>.Failure("Failed to retrieve bookings");
        }
    }

    public async Task<Result<BookingDto>> GetBookingByNumberAsync(
        Guid userId,
        string bookingNumber,
        CancellationToken ct = default)
    {
        try
        {
            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber, ct);

            if (booking is null)
            {
                return Result<BookingDto>.Failure("Booking not found");
            }

            // Verify ownership
            if (booking.UserId != userId)
            {
                return Result<BookingDto>.Failure("Unauthorized");
            }

            var resultDto = new BookingDto(
                booking.Id,
                booking.BookingNumber,
                booking.ShowtimeId,
                booking.Showtime!.Movie!.Title,
                booking.Showtime.StartTime,
                booking.Showtime.CinemaHall!.Name,
                booking.SeatNumbers,
                booking.TotalAmount,
                booking.Status.ToString(),
                booking.BookedAt,
                booking.CarLicensePlate
            );

            return Result<BookingDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking by number {BookingNumber}", bookingNumber);
            return Result<BookingDto>.Failure("Failed to retrieve booking");
        }
    }
}
