using Application.DTOs.Movies;
using Domain.Common;

namespace Application.Services;

public interface IMovieService
{
    Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Result<MovieDto>> GetMovieByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<MovieDto>> CreateMovieAsync(CreateMovieDto dto, CancellationToken ct = default);
    Task<Result<MovieDto>> UpdateMovieAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default);
    Task<Result> DeleteMovieAsync(Guid id, CancellationToken ct = default);
}
