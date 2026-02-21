using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly CinemaDbContext _context;
    private readonly IAuditCaptureService _auditCapture;

    public MovieRepository(CinemaDbContext context, IAuditCaptureService auditCapture)
    {
        _context = context;
        _auditCapture = auditCapture;
    }

    public async Task<List<Movie>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Movies.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync(ct);
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Movie> CreateAsync(Movie movie, CancellationToken ct = default)
    {
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(ct);
        return movie;
    }

    public async Task<Movie> UpdateAsync(Movie movie, CancellationToken ct = default)
    {
        var existing = await _context.Movies.FindAsync([movie.Id], ct);
        if (existing is null)
            return movie;

        // Capture old values from the tracked entry BEFORE SetValues overwrites them.
        // The AuditInterceptor reads these from IAuditCaptureService so it can produce
        // a correct before/after diff without relying on GetDatabaseValuesAsync(),
        // which can be unreliable for record entities on SQL Server (EF Core 9).
        var entry = _context.Entry(existing);
        var oldValues = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        _auditCapture.RegisterPreUpdateValues(typeof(Movie), movie.Id.ToString(), oldValues);

        entry.CurrentValues.SetValues(movie);
        await _context.SaveChangesAsync(ct);
        return movie;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var movie = await _context.Movies.FindAsync([id], ct);
        if (movie is not null)
        {
            // Use the property API so EF Core retains the original IsActive value in the
            // change-tracker snapshot, giving the audit log a correct before/after diff.
            _context.Entry(movie).Property(m => m.IsActive).CurrentValue = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
