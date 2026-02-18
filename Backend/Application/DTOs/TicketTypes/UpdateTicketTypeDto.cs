namespace Application.DTOs.TicketTypes;

public record UpdateTicketTypeDto(
    string Name,
    string Description,
    decimal PriceModifier,
    bool IsActive,
    int SortOrder
);
