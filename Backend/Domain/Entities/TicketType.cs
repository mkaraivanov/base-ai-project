namespace Domain.Entities;

public record TicketType
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Multiplier applied to the seat price. 1.0 = full price, 0.5 = half price, etc.
    /// </summary>
    public decimal PriceModifier { get; init; } = 1.0m;

    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
}
