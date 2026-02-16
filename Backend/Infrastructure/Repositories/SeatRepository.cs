using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly CinemaDbContext _context;

    public SeatRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetByShowtimeIdAsync(Guid showtimeId, CancellationToken ct = default)
    {
        return await _context.Seats
            .AsNoTracking()
            .Where(s => s.ShowtimeId == showtimeId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetByShowtimeAndNumbersAsync(
        Guid showtimeId,
        IReadOnlyList<string> seatNumbers,
        CancellationToken ct = default)
    {
        // CRITICAL: Do NOT use AsNoTracking() here!
        // EF Core needs to track these entities for optimistic concurrency (RowVersion) to work.
        return await _context.Seats
            .Where(s => s.ShowtimeId == showtimeId && seatNumbers.Contains(s.SeatNumber))
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => s.ReservationId == reservationId)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Seat seat, CancellationToken ct = default)
    {
        // For record types that use 'with' expressions, we need to update the tracked entity
        // Use SetValues to copy property values from the new instance to the tracked instance
        // This preserves the RowVersion for optimistic concurrency
        var entry = _context.Entry(seat);
        if (entry.State == EntityState.Detached)
        {
            // Find the tracked entity by ID
            var trackedEntity = await _context.Seats.FindAsync(new object[] { seat.Id });
            if (trackedEntity != null)
            {
                _context.Entry(trackedEntity).CurrentValues.SetValues(seat);
            }
            else
            {
                _context.Seats.Update(seat);
            }
        }
        // If already tracked, EF Core will detect changes automatically
        await Task.CompletedTask;
    }

    public async Task UpdateRangeAsync(List<Seat> seats, CancellationToken ct = default)
    {
        // For record types that use 'with' expressions, we need to update tracked entities
        // Use SetValues to copy property values to preserve RowVersion for optimistic concurrency
        foreach (var seat in seats)
        {
            var entry = _context.Entry(seat);
            if (entry.State == EntityState.Detached)
            {
                // Find the tracked entity by ID
                var trackedEntity = await _context.Seats.FindAsync(new object[] { seat.Id });
                if (trackedEntity != null)
                {
                    _context.Entry(trackedEntity).CurrentValues.SetValues(seat);
                }
                else
                {
                    _context.Seats.Update(seat);
                }
            }
            // If already tracked, EF Core will detect changes automatically
        }
        await Task.CompletedTask;
    }
}
