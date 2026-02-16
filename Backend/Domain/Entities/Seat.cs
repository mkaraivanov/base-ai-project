namespace Domain.Entities;

public record Seat
{
    public Guid Id { get; init; }
    public Guid ShowtimeId { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public string SeatType { get; init; } = "Regular";
    public decimal Price { get; init; }
    public SeatStatus Status { get; init; } = SeatStatus.Available;
    public Guid? ReservationId { get; init; }
    public DateTime? ReservedUntil { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>(); // Optimistic concurrency

    // Navigation
    public Showtime? Showtime { get; init; }
}

public enum SeatStatus
{
    Available = 0,
    Reserved = 1,
    Booked = 2,
    Blocked = 3
}
