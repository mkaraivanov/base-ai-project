using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IMovieRepository
{
    Task<List<Movie>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Movie> CreateAsync(Movie movie, CancellationToken ct = default);
    Task<Movie> UpdateAsync(Movie movie, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
