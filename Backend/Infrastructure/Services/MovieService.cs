using Application.DTOs.Movies;
using Application.Resources;
using Application.Services;
using Backend.Infrastructure.Caching;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<MovieService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ICacheService? _cacheService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private const string MoviesCacheKey = "movies:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public MovieService(
        IMovieRepository movieRepository,
        ILogger<MovieService> logger,
        IStringLocalizer<SharedResource> localizer,
        TimeProvider? timeProvider = null,
        ICacheService? cacheService = null)
    {
        _movieRepository = movieRepository;
        _logger = logger;
        _localizer = localizer;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _cacheService = cacheService;
    }

    public async Task<Result<List<MovieDto>>> GetAllMoviesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"{MoviesCacheKey}:{activeOnly}";
            if (_cacheService is not null)
            {
                var cached = await _cacheService.GetAsync<List<MovieDto>>(cacheKey, ct);
                if (cached is not null)
                {
                    return Result<List<MovieDto>>.Success(cached);
                }
            }

            var movies = await _movieRepository.GetAllAsync(activeOnly, ct);
            var dtos = movies.Select(MapToDto).ToList();

            // Cache the result
            if (_cacheService is not null)
            {
                await _cacheService.SetAsync(cacheKey, dtos, CacheDuration, ct);
            }

            return Result<List<MovieDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movies");
            return Result<List<MovieDto>>.Failure(_localizer["Failed to retrieve movies"]);
        }
    }

    public async Task<Result<MovieDto>> GetMovieByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var movie = await _movieRepository.GetByIdAsync(id, ct);
            if (movie is null)
            {
                return Result<MovieDto>.Failure(_localizer["Movie not found"]);
            }

            return Result<MovieDto>.Success(MapToDto(movie));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie {MovieId}", id);
            return Result<MovieDto>.Failure(_localizer["Failed to retrieve movie"]);
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
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            var created = await _movieRepository.CreateAsync(movie, ct);
            _logger.LogInformation("Movie created: {MovieId} - {Title}", created.Id, created.Title);
            await InvalidateMovieCacheAsync(ct);

            return Result<MovieDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating movie: {Title}", dto.Title);
            return Result<MovieDto>.Failure(_localizer["Failed to create movie"]);
        }
    }

    public async Task<Result<MovieDto>> UpdateMovieAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result<MovieDto>.Failure(_localizer["Movie not found"]);
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
                UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            var result = await _movieRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Movie updated: {MovieId}", id);
            await InvalidateMovieCacheAsync(ct);

            return Result<MovieDto>.Success(MapToDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating movie {MovieId}", id);
            return Result<MovieDto>.Failure(_localizer["Failed to update movie"]);
        }
    }

    public async Task<Result> DeleteMovieAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _movieRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure(_localizer["Movie not found"]);
            }

            await _movieRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Movie deleted: {MovieId}", id);
            await InvalidateMovieCacheAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting movie {MovieId}", id);
            return Result.Failure(_localizer["Failed to delete movie"]);
        }
    }

    private async Task InvalidateMovieCacheAsync(CancellationToken ct)
    {
        if (_cacheService is not null)
        {
            await _cacheService.RemoveAsync($"{MoviesCacheKey}:True", ct);
            await _cacheService.RemoveAsync($"{MoviesCacheKey}:False", ct);
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
