namespace Application.DTOs.Showtimes;

public record ShowtimeDto(
    Guid Id,
    Guid MovieId,
    string MovieTitle,
    Guid CinemaHallId,
    string HallName,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice,
    int AvailableSeats,
    bool IsActive
);
