using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ICinemaHallRepository
{
    Task<List<CinemaHall>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<CinemaHall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CinemaHall> CreateAsync(CinemaHall hall, CancellationToken ct = default);
    Task<CinemaHall> UpdateAsync(CinemaHall hall, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
