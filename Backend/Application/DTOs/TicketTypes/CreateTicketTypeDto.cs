namespace Application.DTOs.TicketTypes;

public record CreateTicketTypeDto(
    string Name,
    string Description,
    decimal PriceModifier,
    int SortOrder
);
