namespace Application.DTOs.Reporting;

public record SalesByMovieDto(
    Guid MovieId,
    string MovieTitle,
    int TicketsSold,
    decimal Revenue,
    int TotalCapacity,
    double CapacityUsedPercent
);
