namespace Domain.Entities;

public class LoyaltySettings
{
    public Guid Id { get; init; }
    public int StampsRequired { get; init; } = 5;
    public DateTime UpdatedAt { get; init; }
}
