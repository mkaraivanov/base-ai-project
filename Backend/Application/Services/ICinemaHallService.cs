using Application.DTOs.CinemaHalls;
using Domain.Common;

namespace Application.Services;

public interface ICinemaHallService
{
    Task<Result<List<CinemaHallDto>>> GetAllHallsAsync(bool activeOnly = true, Guid? cinemaId = null, CancellationToken ct = default);
    Task<Result<CinemaHallDto>> GetHallByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CinemaHallDto>> CreateHallAsync(CreateCinemaHallDto dto, CancellationToken ct = default);
    Task<Result<CinemaHallDto>> UpdateHallAsync(Guid id, UpdateCinemaHallDto dto, CancellationToken ct = default);
    Task<Result> DeleteHallAsync(Guid id, CancellationToken ct = default);
}
