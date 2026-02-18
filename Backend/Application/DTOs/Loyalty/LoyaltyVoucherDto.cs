namespace Application.DTOs.Loyalty;

public record LoyaltyVoucherDto(
    Guid Id,
    string Code,
    bool IsUsed,
    DateTime IssuedAt,
    DateTime? UsedAt
);
