using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingTicketRepository : IBookingTicketRepository
{
    private readonly CinemaDbContext _context;

    public BookingTicketRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task CreateRangeAsync(IEnumerable<BookingTicket> tickets, CancellationToken ct = default)
    {
        _context.BookingTickets.AddRange(tickets);
        // SaveChanges is handled by UnitOfWork.CommitTransactionAsync
        await Task.CompletedTask;
    }

    public async Task<List<BookingTicket>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _context.BookingTickets
            .AsNoTracking()
            .Include(bt => bt.TicketType)
            .Where(bt => bt.BookingId == bookingId)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, List<BookingTicket>>> GetByBookingIdsAsync(
        IEnumerable<Guid> bookingIds,
        CancellationToken ct = default)
    {
        var ids = bookingIds.ToList();
        var tickets = await _context.BookingTickets
            .AsNoTracking()
            .Include(bt => bt.TicketType)
            .Where(bt => ids.Contains(bt.BookingId))
            .ToListAsync(ct);

        return tickets
            .GroupBy(bt => bt.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
