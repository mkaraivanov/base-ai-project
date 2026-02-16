using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByShowtimeIdAsync(Guid showtimeId, CancellationToken ct = default);
    Task<List<Seat>> GetByShowtimeAndNumbersAsync(Guid showtimeId, IReadOnlyList<string> seatNumbers, CancellationToken ct = default);
    Task<List<Seat>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default);
    Task UpdateAsync(Seat seat, CancellationToken ct = default);
    Task UpdateRangeAsync(List<Seat> seats, CancellationToken ct = default);
}
