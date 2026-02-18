using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationTicketRepository : IReservationTicketRepository
{
    private readonly CinemaDbContext _context;

    public ReservationTicketRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task CreateRangeAsync(IEnumerable<ReservationTicket> tickets, CancellationToken ct = default)
    {
        _context.ReservationTickets.AddRange(tickets);
        // SaveChanges is handled by UnitOfWork.CommitTransactionAsync
        await Task.CompletedTask;
    }

    public async Task<List<ReservationTicket>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default)
        => await _context.ReservationTickets
            .AsNoTracking()
            .Include(rt => rt.TicketType)
            .Where(rt => rt.ReservationId == reservationId)
            .ToListAsync(ct);
}
