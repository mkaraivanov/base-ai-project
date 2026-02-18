namespace Application.DTOs.TicketTypes;

public record TicketTypeDto(
    Guid Id,
    string Name,
    string Description,
    decimal PriceModifier,
    bool IsActive,
    int SortOrder
);
