namespace Domain.Entities;

public class LoyaltyCard
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int Stamps { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Navigation
    public User? User { get; init; }
    public IReadOnlyList<LoyaltyVoucher> Vouchers { get; init; } = new List<LoyaltyVoucher>();
}
