namespace Application.DTOs.Reporting;

public record SalesByShowtimeDto(
    Guid ShowtimeId,
    DateTime StartTime,
    string MovieTitle,
    string HallName,
    string CinemaName,
    int TicketsSold,
    int Capacity,
    double OccupancyPercent,
    decimal Revenue
);
