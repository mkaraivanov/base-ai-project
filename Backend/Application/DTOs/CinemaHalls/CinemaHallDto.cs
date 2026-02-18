using Domain.ValueObjects;

namespace Application.DTOs.CinemaHalls;

public record CinemaHallDto(
    Guid Id,
    Guid CinemaId,
    string CinemaName,
    string Name,
    int TotalSeats,
    SeatLayout SeatLayout,
    bool IsActive,
    DateTime CreatedAt
);
