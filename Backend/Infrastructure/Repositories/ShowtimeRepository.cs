using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShowtimeRepository : IShowtimeRepository
{
    private readonly CinemaDbContext _context;

    public ShowtimeRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Showtime>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? cinemaId = null, CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
                .ThenInclude(h => h!.Cinema)
            .Where(s => s.IsActive);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartTime <= toDate.Value);
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(s => s.CinemaHall!.CinemaId == cinemaId.Value);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<Showtime>> GetByMovieIdAsync(Guid movieId, Guid? cinemaId = null, CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
                .ThenInclude(h => h!.Cinema)
            .Where(s => s.MovieId == movieId && s.IsActive);

        if (cinemaId.HasValue)
        {
            query = query.Where(s => s.CinemaHall!.CinemaId == cinemaId.Value);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
                .ThenInclude(h => h!.Cinema)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Showtime> CreateAsync(Showtime showtime, CancellationToken ct = default)
    {
        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task<Showtime> UpdateAsync(Showtime showtime, CancellationToken ct = default)
    {
        DetachTracked(showtime.Id);
        // Null out navigation properties to avoid change tracking conflicts with related entities
        var toUpdate = showtime with { Movie = null, CinemaHall = null };
        _context.Showtimes.Update(toUpdate);
        await _context.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var showtime = await _context.Showtimes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (showtime is not null)
        {
            DetachTracked(id);
            var updated = showtime with { IsActive = false };
            _context.Showtimes.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }

    private void DetachTracked(Guid id)
    {
        var tracked = _context.ChangeTracker.Entries<Showtime>()
            .FirstOrDefault(e => e.Entity.Id == id);
        if (tracked is not null)
            tracked.State = EntityState.Detached;
    }

    public async Task<bool> HasOverlappingShowtimeAsync(
        Guid hallId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeShowtimeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Where(s => s.CinemaHallId == hallId && s.IsActive);

        if (excludeShowtimeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeShowtimeId.Value);
        }

        return await query.AnyAsync(s =>
            (startTime >= s.StartTime && startTime < s.EndTime) ||
            (endTime > s.StartTime && endTime <= s.EndTime) ||
            (startTime <= s.StartTime && endTime >= s.EndTime), ct);
    }
}
