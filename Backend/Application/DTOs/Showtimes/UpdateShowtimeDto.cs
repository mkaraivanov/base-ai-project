namespace Application.DTOs.Showtimes;

public record UpdateShowtimeDto(
    DateTime StartTime,
    decimal BasePrice,
    bool IsActive
);
