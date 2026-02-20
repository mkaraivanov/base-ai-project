using System.Text.Json;
using Application.DTOs.CinemaHalls;
using Application.Resources;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Repositories;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CinemaHallService : ICinemaHallService
{
    private readonly ICinemaHallRepository _hallRepository;
    private readonly ILogger<CinemaHallService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CinemaHallService(
        ICinemaHallRepository hallRepository,
        ILogger<CinemaHallService> logger,
        IStringLocalizer<SharedResource> localizer,
        TimeProvider? timeProvider = null)
    {
        _hallRepository = hallRepository;
        _logger = logger;
        _localizer = localizer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<List<CinemaHallDto>>> GetAllHallsAsync(bool activeOnly = true, Guid? cinemaId = null, CancellationToken ct = default)
    {
        try
        {
            var halls = await _hallRepository.GetAllAsync(activeOnly, cinemaId, ct);
            var dtos = halls.Select(MapToDto).ToList();
            return Result<List<CinemaHallDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cinema halls");
            return Result<List<CinemaHallDto>>.Failure(_localizer["Failed to retrieve cinema halls"]);
        }
    }

    public async Task<Result<CinemaHallDto>> GetHallByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var hall = await _hallRepository.GetByIdAsync(id, ct);
            if (hall is null)
            {
                return Result<CinemaHallDto>.Failure(_localizer["Cinema hall not found"]);
            }

            return Result<CinemaHallDto>.Success(MapToDto(hall));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cinema hall {HallId}", id);
            return Result<CinemaHallDto>.Failure(_localizer["Failed to retrieve cinema hall"]);
        }
    }

    public async Task<Result<CinemaHallDto>> CreateHallAsync(CreateCinemaHallDto dto, CancellationToken ct = default)
    {
        try
        {
            var seatLayoutJson = JsonSerializer.Serialize(dto.SeatLayout);
            var totalSeats = dto.SeatLayout.Seats.Count(s => s.IsAvailable);

            var hall = new CinemaHall
            {
                Id = Guid.NewGuid(),
                CinemaId = dto.CinemaId,
                Name = dto.Name,
                TotalSeats = totalSeats,
                SeatLayoutJson = seatLayoutJson,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            var created = await _hallRepository.CreateAsync(hall, ct);
            _logger.LogInformation("Cinema hall created: {HallId} - {Name}", created.Id, created.Name);

            return Result<CinemaHallDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cinema hall: {Name}", dto.Name);
            return Result<CinemaHallDto>.Failure(_localizer["Failed to create cinema hall"]);
        }
    }

    public async Task<Result<CinemaHallDto>> UpdateHallAsync(Guid id, UpdateCinemaHallDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _hallRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result<CinemaHallDto>.Failure(_localizer["Cinema hall not found"]);
            }

            var seatLayoutJson = JsonSerializer.Serialize(dto.SeatLayout);
            var totalSeats = dto.SeatLayout.Seats.Count(s => s.IsAvailable);

            var updated = existing with
            {
                Name = dto.Name,
                TotalSeats = totalSeats,
                SeatLayoutJson = seatLayoutJson,
                IsActive = dto.IsActive
            };

            var result = await _hallRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Cinema hall updated: {HallId}", id);

            return Result<CinemaHallDto>.Success(MapToDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cinema hall {HallId}", id);
            return Result<CinemaHallDto>.Failure(_localizer["Failed to update cinema hall"]);
        }
    }

    public async Task<Result> DeleteHallAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _hallRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure(_localizer["Cinema hall not found"]);
            }

            await _hallRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Cinema hall deleted: {HallId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cinema hall {HallId}", id);
            return Result.Failure(_localizer["Failed to delete cinema hall"]);
        }
    }

    private static CinemaHallDto MapToDto(CinemaHall hall)
    {
        var seatLayout = JsonSerializer.Deserialize<SeatLayout>(hall.SeatLayoutJson) ?? new SeatLayout();

        return new CinemaHallDto(
            hall.Id,
            hall.CinemaId,
            hall.Cinema?.Name ?? string.Empty,
            hall.Name,
            hall.TotalSeats,
            seatLayout,
            hall.IsActive,
            hall.CreatedAt
        );
    }
}
