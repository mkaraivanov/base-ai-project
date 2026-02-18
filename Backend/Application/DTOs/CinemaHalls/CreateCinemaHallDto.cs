using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CreateCinemaHallDto(
    Guid CinemaId,
    string Name,
    SeatLayout SeatLayout
);
