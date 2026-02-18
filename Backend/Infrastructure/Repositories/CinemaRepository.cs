using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CinemaRepository : ICinemaRepository
{
    private readonly CinemaDbContext _context;

    public CinemaRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Cinema>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Cinemas.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Cinema?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Cinemas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Cinema> CreateAsync(Cinema cinema, CancellationToken ct = default)
    {
        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync(ct);
        return cinema;
    }

    public async Task<Cinema> UpdateAsync(Cinema cinema, CancellationToken ct = default)
    {
        _context.Cinemas.Update(cinema);
        await _context.SaveChangesAsync(ct);
        return cinema;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cinema = await _context.Cinemas.FindAsync([id], ct);
        if (cinema is not null)
        {
            var updated = cinema with { IsActive = false, UpdatedAt = DateTime.UtcNow };
            _context.Cinemas.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetHallCountAsync(Guid cinemaId, CancellationToken ct = default)
    {
        return await _context.CinemaHalls
            .AsNoTracking()
            .CountAsync(h => h.CinemaId == cinemaId && h.IsActive, ct);
    }
}
