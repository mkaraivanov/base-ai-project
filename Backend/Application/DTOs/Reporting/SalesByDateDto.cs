namespace Application.DTOs.Reporting;

public record SalesByDateDto(
    string Period,
    int TicketsSold,
    decimal Revenue,
    int? CompareTicketsSold = null,
    decimal? CompareRevenue = null
);
