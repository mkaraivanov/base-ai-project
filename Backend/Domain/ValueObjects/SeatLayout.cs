namespace Domain.ValueObjects;

public record SeatLayout
{
    public int Rows { get; init; }
    public int SeatsPerRow { get; init; }
    public List<SeatDefinition> Seats { get; init; } = [];
}

public record SeatDefinition
{
    public string SeatNumber { get; init; } = string.Empty; // "A1", "B5", etc.
    public int Row { get; init; }
    public int Column { get; init; }
    public string SeatType { get; init; } = "Regular"; // Regular, Premium, VIP
    public decimal PriceMultiplier { get; init; } = 1.0m;
    public bool IsAvailable { get; init; } = true; // For broken/maintenance seats
}
