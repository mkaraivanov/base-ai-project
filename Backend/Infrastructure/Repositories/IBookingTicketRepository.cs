using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IBookingTicketRepository
{
    Task CreateRangeAsync(IEnumerable<BookingTicket> tickets, CancellationToken ct = default);
    Task<List<BookingTicket>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<Dictionary<Guid, List<BookingTicket>>> GetByBookingIdsAsync(IEnumerable<Guid> bookingIds, CancellationToken ct = default);
}
