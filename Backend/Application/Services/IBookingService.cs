using Application.DTOs.Bookings;
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Domain.Common;

namespace Application.Services;

public interface IBookingService
{
    Task<Result<SeatAvailabilityDto>> GetSeatAvailabilityAsync(Guid showtimeId, CancellationToken ct = default);
    Task<Result<ReservationDto>> CreateReservationAsync(Guid userId, CreateReservationDto dto, CancellationToken ct = default);
    Task<Result> CancelReservationAsync(Guid userId, Guid reservationId, CancellationToken ct = default);
    Task<Result<BookingDto>> ConfirmBookingAsync(Guid userId, ConfirmBookingDto dto, CancellationToken ct = default);
    Task<Result<BookingDto>> CancelBookingAsync(Guid userId, Guid bookingId, CancellationToken ct = default);
    Task<Result<List<BookingDto>>> GetMyBookingsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<BookingDto>> GetBookingByNumberAsync(Guid userId, string bookingNumber, CancellationToken ct = default);
}
