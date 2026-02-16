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
