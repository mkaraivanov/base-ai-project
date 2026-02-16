using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly CinemaDbContext _context;

    public ReservationRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Showtime)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<List<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Showtime)
                .ThenInclude(s => s!.Movie)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Reservation>> GetExpiredReservationsAsync(DateTime currentTime, CancellationToken ct = default)
    {
        // CRITICAL: Do NOT use AsNoTracking() here!
        // These reservations will be updated by the cleanup service.
        return await _context.Reservations
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt < currentTime)
            .ToListAsync(ct);
    }

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Add(reservation);
        // SaveChangesAsync is called by UnitOfWork.CommitTransactionAsync
        await Task.CompletedTask;
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Update(reservation);
        // SaveChangesAsync is called by UnitOfWork.CommitTransactionAsync
        await Task.CompletedTask;
        return reservation;
    }
}
