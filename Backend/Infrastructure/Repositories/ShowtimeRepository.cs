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

    public async Task<List<Showtime>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .Where(s => s.IsActive);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartTime <= toDate.Value);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct = default)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .Where(s => s.MovieId == movieId && s.IsActive)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
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
        _context.Showtimes.Update(showtime);
        await _context.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var showtime = await _context.Showtimes.FindAsync(new object[] { id }, ct);
        if (showtime is not null)
        {
            var updated = showtime with { IsActive = false };
            _context.Showtimes.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
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
