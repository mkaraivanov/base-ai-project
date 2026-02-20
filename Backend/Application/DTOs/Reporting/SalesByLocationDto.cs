namespace Application.DTOs.Reporting;

public record SalesByLocationDto(
    Guid CinemaId,
    string CinemaName,
    string City,
    string Country,
    int TicketsSold,
    decimal Revenue
);
