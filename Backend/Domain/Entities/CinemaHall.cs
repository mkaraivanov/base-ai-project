namespace Domain.Entities;

public record CinemaHall
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public string SeatLayoutJson { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
}
