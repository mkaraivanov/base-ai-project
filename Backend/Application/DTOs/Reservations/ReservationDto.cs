namespace Application.DTOs.Reservations;

public record ReservationDto(
    Guid Id,
    Guid ShowtimeId,
    IReadOnlyList<string> SeatNumbers,
    decimal TotalAmount,
    DateTime ExpiresAt,
    string Status,
    DateTime CreatedAt
);
