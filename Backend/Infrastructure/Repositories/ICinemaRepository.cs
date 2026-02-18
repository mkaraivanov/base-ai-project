using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ICinemaRepository
{
    Task<List<Cinema>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Cinema?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cinema> CreateAsync(Cinema cinema, CancellationToken ct = default);
    Task<Cinema> UpdateAsync(Cinema cinema, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetHallCountAsync(Guid cinemaId, CancellationToken ct = default);
}
