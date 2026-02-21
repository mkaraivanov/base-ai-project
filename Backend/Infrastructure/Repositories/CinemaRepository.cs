using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CinemaRepository : ICinemaRepository
{
    private readonly CinemaDbContext _context;
    private readonly IAuditCaptureService _auditCapture;

    public CinemaRepository(CinemaDbContext context, IAuditCaptureService auditCapture)
    {
        _context = context;
        _auditCapture = auditCapture;
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
        var existing = await _context.Cinemas.FindAsync([cinema.Id], ct);
        if (existing is null)
            return cinema;

        // Capture old values from the tracked entry BEFORE SetValues overwrites them.
        // The AuditInterceptor reads these from IAuditCaptureService so it can produce
        // a correct before/after diff without relying on GetDatabaseValuesAsync(),
        // which can be unreliable for record entities on SQL Server (EF Core 9).
        var entry = _context.Entry(existing);
        var oldValues = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        _auditCapture.RegisterPreUpdateValues(typeof(Cinema), cinema.Id.ToString(), oldValues);

        entry.CurrentValues.SetValues(cinema);
        await _context.SaveChangesAsync(ct);
        return cinema;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cinema = await _context.Cinemas.FindAsync([id], ct);
        if (cinema is not null)
        {
            // Use the property API so EF Core retains the original values in the
            // change-tracker snapshot, giving the audit log a correct before/after diff.
            var entry = _context.Entry(cinema);
            entry.Property(c => c.IsActive).CurrentValue = false;
            entry.Property(c => c.UpdatedAt).CurrentValue = DateTime.UtcNow;
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
