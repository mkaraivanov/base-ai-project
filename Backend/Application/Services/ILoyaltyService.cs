using Application.DTOs.Loyalty;
using Domain.Common;

namespace Application.Services;

public interface ILoyaltyService
{
    Task<Result<LoyaltyCardDto>> GetLoyaltyCardAsync(Guid userId, CancellationToken ct = default);
    Task AddStampAsync(Guid userId, CancellationToken ct = default);
    Task<Result<LoyaltySettingsDto>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result<LoyaltySettingsDto>> UpdateSettingsAsync(UpdateLoyaltySettingsDto dto, CancellationToken ct = default);
}
