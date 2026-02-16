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
