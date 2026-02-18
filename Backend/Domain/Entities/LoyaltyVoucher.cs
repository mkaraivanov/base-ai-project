namespace Domain.Entities;

public class LoyaltyVoucher
{
    public Guid Id { get; init; }
    public Guid LoyaltyCardId { get; init; }
    public Guid UserId { get; init; }
    public string Code { get; init; } = string.Empty;
    public bool IsUsed { get; init; }
    public DateTime IssuedAt { get; init; }
    public DateTime? UsedAt { get; init; }

    // Navigation
    public LoyaltyCard? LoyaltyCard { get; init; }
}
