namespace Domain.Entities;

public class Booking
{
    public Guid Id { get; init; }
    public string BookingNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid ShowtimeId { get; init; }
    public List<string> SeatNumbers { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public BookingStatus Status { get; init; } = BookingStatus.Confirmed;
    public Guid? PaymentId { get; init; }
    public DateTime BookedAt { get; init; }
    public DateTime? CancelledAt { get; init; }

    // Navigation
    public User? User { get; init; }
    public Showtime? Showtime { get; init; }
    public Payment? Payment { get; init; }
    // Must use List<T> (not array / Array.Empty) so EF Core navigation fixup can
    // call Add() when inserting related BookingTicket entities in the same context.
    public IReadOnlyList<BookingTicket> Tickets { get; init; } = new List<BookingTicket>();
}

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1,
    Refunded = 2
}
