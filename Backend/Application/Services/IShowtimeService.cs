using Application.DTOs.Showtimes;
using Domain.Common;

namespace Application.Services;

public interface IShowtimeService
{
    Task<Result<List<ShowtimeDto>>> GetShowtimesAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? cinemaId = null, CancellationToken ct = default);
    Task<Result<List<ShowtimeDto>>> GetShowtimesByMovieAsync(Guid movieId, Guid? cinemaId = null, CancellationToken ct = default);
    Task<Result<ShowtimeDto>> GetShowtimeByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ShowtimeDto>> CreateShowtimeAsync(CreateShowtimeDto dto, CancellationToken ct = default);
    Task<Result> DeleteShowtimeAsync(Guid id, CancellationToken ct = default);
}
