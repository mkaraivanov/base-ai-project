using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CinemaHallDto(
    Guid Id,
    string Name,
    int TotalSeats,
    SeatLayout SeatLayout,
    bool IsActive,
    DateTime CreatedAt
);
