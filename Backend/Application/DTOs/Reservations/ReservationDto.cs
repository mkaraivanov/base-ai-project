using Application.DTOs.Bookings;

namespace Application.DTOs.Reservations;

public record ReservationDto(
    Guid Id,
    Guid ShowtimeId,
    IReadOnlyList<string> SeatNumbers,
    IReadOnlyList<TicketLineItemDto> Tickets,
    decimal TotalAmount,
    DateTime ExpiresAt,
    string Status,
    DateTime CreatedAt
);
