namespace Application.DTOs.Showtimes;

public record CreateShowtimeDto(
    Guid MovieId,
    Guid CinemaHallId,
    DateTime StartTime,
    decimal BasePrice
);
