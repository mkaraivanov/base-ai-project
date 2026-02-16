namespace Application.DTOs.Seats;

public record SeatAvailabilityDto(
    Guid ShowtimeId,
    List<SeatDto> AvailableSeats,
    List<SeatDto> ReservedSeats,
    List<SeatDto> BookedSeats,
    int TotalSeats
);

public record SeatDto(
    string SeatNumber,
    string SeatType,
    decimal Price,
    string Status
);
