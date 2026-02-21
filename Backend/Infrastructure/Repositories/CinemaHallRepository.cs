using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CinemaHallRepository : ICinemaHallRepository
{
    private readonly CinemaDbContext _context;
    private readonly IAuditCaptureService _auditCapture;

    public CinemaHallRepository(CinemaDbContext context, IAuditCaptureService auditCapture)
    {
        _context = context;
        _auditCapture = auditCapture;
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
        var existing = await _context.CinemaHalls.FindAsync([hall.Id], ct);
        if (existing is null)
            return hall;

        // Capture old values from the tracked entry BEFORE SetValues overwrites them.
        // The AuditInterceptor reads these from IAuditCaptureService so it can produce
        // a correct before/after diff without relying on GetDatabaseValuesAsync(),
        // which can be unreliable for record entities on SQL Server (EF Core 9).
        var entry = _context.Entry(existing);
        var oldValues = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        _auditCapture.RegisterPreUpdateValues(typeof(CinemaHall), hall.Id.ToString(), oldValues);

        entry.CurrentValues.SetValues(hall);
        await _context.SaveChangesAsync(ct);
        return hall;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hall = await _context.CinemaHalls.FindAsync([id], ct);
        if (hall is not null)
        {
            // Use the property API so EF Core retains the original IsActive value in the
            // change-tracker snapshot, giving the audit log a correct before/after diff.
            _context.Entry(hall).Property(h => h.IsActive).CurrentValue = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
