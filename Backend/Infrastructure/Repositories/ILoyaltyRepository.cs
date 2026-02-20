using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ILoyaltyRepository
{
    Task<LoyaltyCard?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<LoyaltyCard> CreateAsync(LoyaltyCard card, CancellationToken ct = default);
    Task<LoyaltyCard> UpdateAsync(LoyaltyCard card, CancellationToken ct = default);
    Task<LoyaltyVoucher> CreateVoucherAsync(LoyaltyVoucher voucher, CancellationToken ct = default);
    Task DeleteOldestUnusedVoucherAsync(Guid userId, CancellationToken ct = default);
    Task<LoyaltySettings?> GetSettingsAsync(CancellationToken ct = default);
    Task<LoyaltySettings> UpsertSettingsAsync(LoyaltySettings settings, CancellationToken ct = default);
}
