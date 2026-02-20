using System.Text.Json;
using Application.DTOs.Showtimes;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ShowtimeService : IShowtimeService
{
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ICinemaHallRepository _hallRepository;
    private readonly CinemaDbContext _context;
    private readonly ILogger<ShowtimeService> _logger;
    private readonly TimeProvider _timeProvider;

    public ShowtimeService(
        IShowtimeRepository showtimeRepository,
        IMovieRepository movieRepository,
        ICinemaHallRepository hallRepository,
        CinemaDbContext context,
        ILogger<ShowtimeService> logger,
        TimeProvider? timeProvider = null)
    {
        _showtimeRepository = showtimeRepository;
        _movieRepository = movieRepository;
        _hallRepository = hallRepository;
        _context = context;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<List<ShowtimeDto>>> GetShowtimesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? cinemaId = null,
        CancellationToken ct = default)
    {
        try
        {
            var showtimes = await _showtimeRepository.GetAllAsync(fromDate, toDate, cinemaId, ct);
            var dtos = await MapToShowtimeDtosAsync(showtimes, ct);
            return Result<List<ShowtimeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving showtimes");
            return Result<List<ShowtimeDto>>.Failure("Failed to retrieve showtimes");
        }
    }

    public async Task<Result<List<ShowtimeDto>>> GetShowtimesByMovieAsync(Guid movieId, Guid? cinemaId = null, CancellationToken ct = default)
    {
        try
        {
            var showtimes = await _showtimeRepository.GetByMovieIdAsync(movieId, cinemaId, ct);
            var dtos = await MapToShowtimeDtosAsync(showtimes, ct);
            return Result<List<ShowtimeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving showtimes for movie {MovieId}", movieId);
            return Result<List<ShowtimeDto>>.Failure("Failed to retrieve showtimes");
        }
    }

    public async Task<Result<ShowtimeDto>> GetShowtimeByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var showtime = await _showtimeRepository.GetByIdAsync(id, ct);
            if (showtime is null)
            {
                return Result<ShowtimeDto>.Failure("Showtime not found");
            }

            var availableSeats = await GetAvailableSeatCountAsync(id, ct);
            var dto = MapToShowtimeDto(showtime, availableSeats);
            return Result<ShowtimeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving showtime {ShowtimeId}", id);
            return Result<ShowtimeDto>.Failure("Failed to retrieve showtime");
        }
    }

    public async Task<Result<ShowtimeDto>> CreateShowtimeAsync(CreateShowtimeDto dto, CancellationToken ct = default)
    {
        try
        {
            // Validate movie exists
            var movie = await _movieRepository.GetByIdAsync(dto.MovieId, ct);
            if (movie is null)
            {
                return Result<ShowtimeDto>.Failure("Movie not found");
            }

            // Validate cinema hall exists
            var hall = await _hallRepository.GetByIdAsync(dto.CinemaHallId, ct);
            if (hall is null)
            {
                return Result<ShowtimeDto>.Failure("Cinema hall not found");
            }

            // Calculate end time (movie duration + 30 min buffer for cleaning)
            var endTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30);

            // Check for overlapping showtimes
            var hasOverlap = await _showtimeRepository.HasOverlappingShowtimeAsync(
                dto.CinemaHallId,
                dto.StartTime,
                endTime,
                null,
                ct);

            if (hasOverlap)
            {
                return Result<ShowtimeDto>.Failure("Showtime overlaps with existing showtime in this hall");
            }

            // Create showtime
            var showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                MovieId = dto.MovieId,
                CinemaHallId = dto.CinemaHallId,
                StartTime = dto.StartTime,
                EndTime = endTime,
                BasePrice = dto.BasePrice,
                IsActive = true,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            // Use transaction to create showtime and seats atomically
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var created = await _showtimeRepository.CreateAsync(showtime, ct);

                // Generate seats for this showtime
                await GenerateSeatsForShowtimeAsync(created.Id, hall, dto.BasePrice, ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Showtime created: {ShowtimeId} for movie {MovieId} in hall {HallId}",
                    created.Id,
                    dto.MovieId,
                    dto.CinemaHallId);

                var resultDto = new ShowtimeDto(
                    created.Id,
                    movie.Id,
                    movie.Title,
                    hall.Id,
                    hall.Name,
                    hall.CinemaId,
                    hall.Cinema?.Name ?? string.Empty,
                    created.StartTime,
                    created.EndTime,
                    created.BasePrice,
                    hall.TotalSeats,
                    created.IsActive
                );

                return Result<ShowtimeDto>.Success(resultDto);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating showtime");
            return Result<ShowtimeDto>.Failure("Failed to create showtime");
        }
    }

    public async Task<Result<ShowtimeDto>> UpdateShowtimeAsync(Guid id, UpdateShowtimeDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _showtimeRepository.GetByIdAsync(id, ct);
            if (existing is null)
                return Result<ShowtimeDto>.Failure("Showtime not found");

            var movie = await _movieRepository.GetByIdAsync(existing.MovieId, ct);
            if (movie is null)
                return Result<ShowtimeDto>.Failure("Associated movie not found");

            var newEndTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30);

            var hasOverlap = await _showtimeRepository.HasOverlappingShowtimeAsync(
                existing.CinemaHallId,
                dto.StartTime,
                newEndTime,
                excludeShowtimeId: id,
                ct);

            if (hasOverlap)
                return Result<ShowtimeDto>.Failure("Showtime overlaps with existing showtime in this hall");

            var updated = existing with
            {
                StartTime = dto.StartTime,
                EndTime = newEndTime,
                BasePrice = dto.BasePrice,
                IsActive = dto.IsActive
            };

            var saved = await _showtimeRepository.UpdateAsync(updated, ct);
            _logger.LogInformation("Showtime updated: {ShowtimeId}", id);

            var availableSeats = await GetAvailableSeatCountAsync(saved.Id, ct);
            return Result<ShowtimeDto>.Success(MapToShowtimeDto(saved, availableSeats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating showtime {ShowtimeId}", id);
            return Result<ShowtimeDto>.Failure("Failed to update showtime");
        }
    }

    public async Task<Result> DeleteShowtimeAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _showtimeRepository.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Result.Failure("Showtime not found");
            }

            await _showtimeRepository.DeleteAsync(id, ct);
            _logger.LogInformation("Showtime deleted: {ShowtimeId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting showtime {ShowtimeId}", id);
            return Result.Failure("Failed to delete showtime");
        }
    }

    private async Task GenerateSeatsForShowtimeAsync(
        Guid showtimeId,
        CinemaHall hall,
        decimal basePrice,
        CancellationToken ct)
    {
        var seatLayout = JsonSerializer.Deserialize<SeatLayout>(hall.SeatLayoutJson);
        if (seatLayout is null)
        {
            throw new InvalidOperationException("Invalid seat layout JSON");
        }

        var seats = seatLayout.Seats
            .Where(s => s.IsAvailable)
            .Select(s => new Seat
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                SeatNumber = s.SeatNumber,
                SeatType = s.SeatType,
                Price = basePrice * s.PriceMultiplier,
                Status = SeatStatus.Available,
                ReservationId = null,
                ReservedUntil = null,
                RowVersion = Array.Empty<byte>()
            })
            .ToList();

        _context.Seats.AddRange(seats);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Generated {SeatCount} seats for showtime {ShowtimeId}", seats.Count, showtimeId);
    }

    private async Task<List<ShowtimeDto>> MapToShowtimeDtosAsync(List<Showtime> showtimes, CancellationToken ct)
    {
        var result = new List<ShowtimeDto>();

        foreach (var showtime in showtimes)
        {
            var availableSeats = await GetAvailableSeatCountAsync(showtime.Id, ct);
            result.Add(MapToShowtimeDto(showtime, availableSeats));
        }

        return result;
    }

    private static ShowtimeDto MapToShowtimeDto(Showtime showtime, int availableSeats) => new(
        showtime.Id,
        showtime.MovieId,
        showtime.Movie?.Title ?? string.Empty,
        showtime.CinemaHallId,
        showtime.CinemaHall?.Name ?? string.Empty,
        showtime.CinemaHall?.CinemaId ?? Guid.Empty,
        showtime.CinemaHall?.Cinema?.Name ?? string.Empty,
        showtime.StartTime,
        showtime.EndTime,
        showtime.BasePrice,
        availableSeats,
        showtime.IsActive
    );

    private async Task<int> GetAvailableSeatCountAsync(Guid showtimeId, CancellationToken ct)
    {
        return await _context.Seats
            .AsNoTracking()
            .Where(s => s.ShowtimeId == showtimeId && s.Status == SeatStatus.Available)
            .CountAsync(ct);
    }
}
