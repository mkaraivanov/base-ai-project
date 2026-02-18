using Application.DTOs.Cinemas;
using Domain.Common;

namespace Application.Services;

public interface ICinemaService
{
    Task<Result<List<CinemaDto>>> GetAllCinemasAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Result<CinemaDto>> GetCinemaByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CinemaDto>> CreateCinemaAsync(CreateCinemaDto dto, CancellationToken ct = default);
    Task<Result<CinemaDto>> UpdateCinemaAsync(Guid id, UpdateCinemaDto dto, CancellationToken ct = default);
    Task<Result> DeleteCinemaAsync(Guid id, CancellationToken ct = default);
}
