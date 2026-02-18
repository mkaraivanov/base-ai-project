namespace Domain.Entities;

public class ReservationTicket
{
    public Guid Id { get; init; }
    public Guid ReservationId { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public Guid TicketTypeId { get; init; }

    /// <summary>Raw seat price before the ticket-type modifier is applied.</summary>
    public decimal SeatPrice { get; init; }

    /// <summary>Final price: SeatPrice × TicketType.PriceModifier.</summary>
    public decimal UnitPrice { get; init; }

    // Navigation
    public TicketType? TicketType { get; init; }
}
