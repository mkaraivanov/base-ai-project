using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IShowtimeRepository
{
    Task<List<Showtime>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<List<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct = default);
    Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Showtime> CreateAsync(Showtime showtime, CancellationToken ct = default);
    Task<Showtime> UpdateAsync(Showtime showtime, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasOverlappingShowtimeAsync(Guid hallId, DateTime startTime, DateTime endTime, Guid? excludeShowtimeId = null, CancellationToken ct = default);
}
