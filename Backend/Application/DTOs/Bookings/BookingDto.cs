namespace Application.DTOs.Bookings;

public record BookingDto(
    Guid Id,
    string BookingNumber,
    Guid ShowtimeId,
    string MovieTitle,
    DateTime ShowtimeStart,
    string HallName,
    List<string> SeatNumbers,
    IReadOnlyList<TicketLineItemDto> Tickets,
    decimal TotalAmount,
    string Status,
    DateTime BookedAt,
    string? CarLicensePlate = null
);
