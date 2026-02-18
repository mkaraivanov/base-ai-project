namespace Application.DTOs.Loyalty;

public record LoyaltySettingsDto(int StampsRequired);

public record UpdateLoyaltySettingsDto(int StampsRequired);
