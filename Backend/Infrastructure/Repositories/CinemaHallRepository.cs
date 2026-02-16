using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CinemaHallRepository : ICinemaHallRepository
{
    private readonly CinemaDbContext _context;

    public CinemaHallRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<CinemaHall>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.CinemaHalls.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        return await query.OrderBy(h => h.Name).ToListAsync(ct);
    }

    public async Task<CinemaHall?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CinemaHalls
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task<CinemaHall> CreateAsync(CinemaHall hall, CancellationToken ct = default)
    {
        _context.CinemaHalls.Add(hall);
        await _context.SaveChangesAsync(ct);
        return hall;
    }

    public async Task<CinemaHall> UpdateAsync(CinemaHall hall, CancellationToken ct = default)
    {
        _context.CinemaHalls.Update(hall);
        await _context.SaveChangesAsync(ct);
        return hall;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hall = await _context.CinemaHalls.FindAsync([id], ct);
        if (hall is not null)
        {
            // Soft delete
            var updated = hall with { IsActive = false };
            _context.CinemaHalls.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }
}
