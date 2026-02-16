using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly CinemaDbContext _context;

    public MovieRepository(CinemaDbContext context)
    {
        _context = context;
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
        _context.Movies.Update(movie);
        await _context.SaveChangesAsync(ct);
        return movie;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var movie = await _context.Movies.FindAsync([id], ct);
        if (movie is not null)
        {
            // Soft delete
            var updated = movie with { IsActive = false };
            _context.Movies.Update(updated);
            await _context.SaveChangesAsync(ct);
        }
    }
}
