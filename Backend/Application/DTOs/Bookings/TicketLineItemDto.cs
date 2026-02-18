namespace Application.DTOs.Bookings;

public record TicketLineItemDto(
    string SeatNumber,
    string SeatType,
    string TicketTypeName,
    decimal SeatPrice,
    decimal UnitPrice
);
