namespace Application.DTOs.Loyalty;

public record LoyaltyCardDto(
    Guid Id,
    int Stamps,
    int StampsRequired,
    int StampsRemaining,
    IReadOnlyList<LoyaltyVoucherDto> ActiveVouchers
);
