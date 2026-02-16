using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CreateCinemaHallDto(
    string Name,
    SeatLayout SeatLayout
);
