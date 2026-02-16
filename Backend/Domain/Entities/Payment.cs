namespace Domain.Entities;

public class Payment
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty; // CreditCard, Mock, etc.
    public string TransactionId { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }

    // Navigation
    public Booking? Booking { get; init; }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
