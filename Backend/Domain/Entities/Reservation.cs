namespace Domain.Entities;

public record Reservation
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ShowtimeId { get; init; }
    public IReadOnlyList<string> SeatNumbers { get; init; } = Array.Empty<string>();
    public decimal TotalAmount { get; init; }
    public DateTime ExpiresAt { get; init; }
    public ReservationStatus Status { get; init; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; init; }

    // Navigation properties
    public User? User { get; init; }
    public Showtime? Showtime { get; init; }
}

public enum ReservationStatus
{
    Pending = 0,
    Expired = 1,
    Confirmed = 2,
    Cancelled = 3
}
