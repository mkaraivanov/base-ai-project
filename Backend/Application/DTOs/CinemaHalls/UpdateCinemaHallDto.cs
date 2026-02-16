using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record UpdateCinemaHallDto(
    string Name,
    SeatLayout SeatLayout,
    bool IsActive
);
