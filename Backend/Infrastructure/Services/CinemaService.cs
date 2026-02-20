using Application.DTOs.Cinemas;
using Application.Resources;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _cinemaRepository;
    private readonly ILogger<CinemaService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CinemaService(
        ICinemaRepository cinemaRepository,
        ILogger<CinemaService> logger,
        IStringLocalizer<SharedResource> localizer,
        TimeProvider? timeProvider = null)
    {
        _cinemaRepository = cinemaRepository;
        _logger = logger;
        _localizer = localizer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<List<CinemaDto>>> GetAllCinemasAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        try
        {
            var cinemas = await _cinemaRepository.GetAllAsync(activeOnly, ct);
            var dtos = new List<CinemaDto>(cinemas.Count);
            foreach (var c in cinemas)
            {
                var hallCount = await _cinemaRepository.GetHallCountAsync(c.Id, ct);
                dtos.Add(MapToDto(c, hallCount));
            }
            return Result<List<CinemaDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cinemas");
            return Result<List<CinemaDto>>.Failure(_localizer["Failed to retrieve cinemas"]);
        }
    }

    public async Task<Result<CinemaDto>> GetCinemaByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var cinema = await _cinemaRepository.GetByIdAsync(id, ct);
            if (cinema is null)
            {
                return Result<CinemaDto>.Failure(_localizer["Cinema not found"]);
            }

            var hallCount = await _cinemaRepository.GetHallCountAsync(id, ct);
            return Result<CinemaDto>.Success(MapToDto(cinema, hallCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cinema {CinemaId}", id);
            return Result<CinemaDto>.Failure(_localizer["Failed to retrieve cinema"]);
        }
    }

    public async Task<Result<CinemaDto>> CreateCinemaAsync(CreateCinemaDto dto, CancellationToken ct = default)
    {
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var cinema = new Cinema
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                Country = dto.Country,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                LogoUrl = dto.LogoUrl,
                OpenTime = dto.OpenTime,
                CloseTime = dto.CloseTime,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _cinemaRepository.CreateAsync(cinema, ct);
            _logger.LogInformation("Cinema created: {CinemaId} - {Name}", created.Id, created.Name);

            return Result<CinemaDto>.Success(MapToDto(created, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cinema: {Name}", dto.Name);
            return Result<CinemaDto>.Failure(_localizer["Failed to create cinema"]);
        }
    }

    public async Task<Result<CinemaDto>> UpdateCinemaAsync(Guid id, UpdateCinemaDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _cinemaRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result<CinemaDto>.Failure(_localizer["Cinema not found"]);
            }

            var updated = existing with
            {
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                Country = dto.Country,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                LogoUrl = dto.LogoUrl,
                OpenTime = dto.OpenTime,
                CloseTime = dto.CloseTime,
                IsActive = dto.IsActive,
                UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            var result = await _cinemaRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Cinema updated: {CinemaId}", id);

            var hallCount = await _cinemaRepository.GetHallCountAsync(id, ct);
            return Result<CinemaDto>.Success(MapToDto(result, hallCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cinema {CinemaId}", id);
            return Result<CinemaDto>.Failure(_localizer["Failed to update cinema"]);
        }
    }

    public async Task<Result> DeleteCinemaAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _cinemaRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure(_localizer["Cinema not found"]);
            }

            await _cinemaRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Cinema deleted: {CinemaId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cinema {CinemaId}", id);
            return Result.Failure(_localizer["Failed to delete cinema"]);
        }
    }

    private static CinemaDto MapToDto(Cinema cinema, int hallCount) => new(
        cinema.Id,
        cinema.Name,
        cinema.Address,
        cinema.City,
        cinema.Country,
        cinema.PhoneNumber,
        cinema.Email,
        cinema.LogoUrl,
        cinema.OpenTime,
        cinema.CloseTime,
        cinema.IsActive,
        cinema.CreatedAt,
        cinema.UpdatedAt,
        hallCount
    );
}
