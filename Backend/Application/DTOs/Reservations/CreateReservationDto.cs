namespace Application.DTOs.Reservations;

public record SeatSelectionDto(
    string SeatNumber,
    Guid TicketTypeId
);

public record CreateReservationDto(
    Guid ShowtimeId,
    IReadOnlyList<SeatSelectionDto> Seats
);
