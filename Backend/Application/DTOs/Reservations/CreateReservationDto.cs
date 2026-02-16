namespace Application.DTOs.Reservations;

public record CreateReservationDto(
    Guid ShowtimeId,
    IReadOnlyList<string> SeatNumbers
);
