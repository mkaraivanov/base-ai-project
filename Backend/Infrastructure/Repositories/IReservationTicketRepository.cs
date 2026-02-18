using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IReservationTicketRepository
{
    Task CreateRangeAsync(IEnumerable<ReservationTicket> tickets, CancellationToken ct = default);
    Task<List<ReservationTicket>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default);
}
