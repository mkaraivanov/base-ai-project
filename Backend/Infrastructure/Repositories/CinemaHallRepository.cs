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

    public async Task<List<CinemaHall>> GetAllAsync(bool activeOnly = true, Guid? cinemaId = null, CancellationToken ct = default)
    {
        IQueryable<CinemaHall> query = _context.CinemaHalls
            .AsNoTracking()
            .Include(h => h.Cinema);

        if (activeOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(h => h.CinemaId == cinemaId.Value);
        }

        return await query.OrderBy(h => h.Name).ToListAsync(ct);
    }

    public async Task<CinemaHall?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CinemaHalls
            .AsNoTracking()
            .Include(h => h.Cinema)
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
        await _context.CinemaHalls
            .Where(h => h.Id == hall.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Name, hall.Name)
                .SetProperty(h => h.TotalSeats, hall.TotalSeats)
                .SetProperty(h => h.SeatLayoutJson, hall.SeatLayoutJson)
                .SetProperty(h => h.IsActive, hall.IsActive),
                ct);
        return hall;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _context.CinemaHalls
            .Where(h => h.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.IsActive, false), ct);
    }
}
