namespace Domain.Entities;

public record Showtime
{
    public Guid Id { get; init; }
    public Guid MovieId { get; init; }
    public Guid CinemaHallId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public decimal BasePrice { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }

    // Navigation properties
    public Movie? Movie { get; init; }
    public CinemaHall? CinemaHall { get; init; }
}
