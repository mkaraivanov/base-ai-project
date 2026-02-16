using Application.DTOs.Movies;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<MovieService> _logger;
    private readonly TimeProvider _timeProvider;

    public MovieService(
        IMovieRepository movieRepository,
        ILogger<MovieService> logger,
        TimeProvider? timeProvider = null)
    {
        _movieRepository = movieRepository;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        try
        {
            var movies = await _movieRepository.GetAllAsync(activeOnly, ct);
            var dtos = movies.Select(MapToDto).ToList();
            return Result<List<MovieDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movies");
            return Result<List<MovieDto>>.Failure("Failed to retrieve movies");
        }
    }

    public async Task<Result<MovieDto>> GetMovieByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var movie = await _movieRepository.GetByIdAsync(id, ct);
            if (movie is null)
            {
                return Result<MovieDto>.Failure("Movie not found");
            }

            return Result<MovieDto>.Success(MapToDto(movie));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie {MovieId}", id);
            return Result<MovieDto>.Failure("Failed to retrieve movie");
        }
    }

    public async Task<Result<MovieDto>> CreateMovieAsync(CreateMovieDto dto, CancellationToken ct = default)
    {
        try
        {
            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Genre = dto.Genre,
                DurationMinutes = dto.DurationMinutes,
                Rating = dto.Rating,
                PosterUrl = dto.PosterUrl,
                ReleaseDate = dto.ReleaseDate,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().DateTime,
                UpdatedAt = _timeProvider.GetUtcNow().DateTime
            };

            var created = await _movieRepository.CreateAsync(movie, ct);
            _logger.LogInformation("Movie created: {MovieId} - {Title}", created.Id, created.Title);

            return Result<MovieDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating movie: {Title}", dto.Title);
            return Result<MovieDto>.Failure("Failed to create movie");
        }
    }

    public async Task<Result<MovieDto>> UpdateMovieAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result<MovieDto>.Failure("Movie not found");
            }

            var updated = existing with
            {
                Title = dto.Title,
                Description = dto.Description,
                Genre = dto.Genre,
                DurationMinutes = dto.DurationMinutes,
                Rating = dto.Rating,
                PosterUrl = dto.PosterUrl,
                ReleaseDate = dto.ReleaseDate,
                IsActive = dto.IsActive,
                UpdatedAt = _timeProvider.GetUtcNow().DateTime
            };

            var result = await _movieRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Movie updated: {MovieId}", id);

            return Result<MovieDto>.Success(MapToDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating movie {MovieId}", id);
            return Result<MovieDto>.Failure("Failed to update movie");
        }
    }

    public async Task<Result> DeleteMovieAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure("Movie not found");
            }

            await _movieRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Movie deleted: {MovieId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting movie {MovieId}", id);
            return Result.Failure("Failed to delete movie");
        }
    }

    private static MovieDto MapToDto(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Description,
        movie.Genre,
        movie.DurationMinutes,
        movie.Rating,
        movie.PosterUrl,
        movie.ReleaseDate,
        movie.IsActive,
        movie.CreatedAt
    );
}
