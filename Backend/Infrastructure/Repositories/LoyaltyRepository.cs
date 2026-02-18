using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LoyaltyRepository : ILoyaltyRepository
{
    private readonly CinemaDbContext _context;

    public LoyaltyRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<LoyaltyCard?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.LoyaltyCards
            .AsNoTracking()
            .Include(c => c.Vouchers)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }

    public async Task<LoyaltyCard> CreateAsync(LoyaltyCard card, CancellationToken ct = default)
    {
        _context.LoyaltyCards.Add(card);
        await _context.SaveChangesAsync(ct);
        return card;
    }

    public async Task<LoyaltyCard> UpdateAsync(LoyaltyCard card, CancellationToken ct = default)
    {
        _context.LoyaltyCards.Update(card);
        await _context.SaveChangesAsync(ct);
        return card;
    }

    public async Task<LoyaltyVoucher> CreateVoucherAsync(LoyaltyVoucher voucher, CancellationToken ct = default)
    {
        _context.LoyaltyVouchers.Add(voucher);
        await _context.SaveChangesAsync(ct);
        return voucher;
    }

    public async Task<LoyaltySettings?> GetSettingsAsync(CancellationToken ct = default)
    {
        return await _context.LoyaltySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LoyaltySettings> UpsertSettingsAsync(LoyaltySettings settings, CancellationToken ct = default)
    {
        var existing = await _context.LoyaltySettings.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            _context.LoyaltySettings.Add(settings);
        }
        else
        {
            _context.LoyaltySettings.Update(settings);
        }
        await _context.SaveChangesAsync(ct);
        return settings;
    }
}
