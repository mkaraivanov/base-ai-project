using Application.DTOs.Loyalty;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    private const int DefaultStampsRequired = 5;

    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly ILogger<LoyaltyService> _logger;
    private readonly TimeProvider _timeProvider;

    public LoyaltyService(
        ILoyaltyRepository loyaltyRepository,
        ILogger<LoyaltyService> logger,
        TimeProvider? timeProvider = null)
    {
        _loyaltyRepository = loyaltyRepository;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<LoyaltyCardDto>> GetLoyaltyCardAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var settings = await _loyaltyRepository.GetSettingsAsync(ct);
            var stampsRequired = settings?.StampsRequired ?? DefaultStampsRequired;

            var card = await _loyaltyRepository.GetByUserIdAsync(userId, ct);
            if (card is null)
            {
                var dto = new LoyaltyCardDto(
                    Guid.Empty,
                    0,
                    stampsRequired,
                    stampsRequired,
                    new List<LoyaltyVoucherDto>()
                );
                return Result<LoyaltyCardDto>.Success(dto);
            }

            var activeVouchers = card.Vouchers
                .Where(v => !v.IsUsed)
                .Select(v => new LoyaltyVoucherDto(v.Id, v.Code, v.IsUsed, v.IssuedAt, v.UsedAt))
                .ToList();

            var cardDto = new LoyaltyCardDto(
                card.Id,
                card.Stamps,
                stampsRequired,
                Math.Max(0, stampsRequired - card.Stamps),
                activeVouchers
            );

            return Result<LoyaltyCardDto>.Success(cardDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty card for user {UserId}", userId);
            return Result<LoyaltyCardDto>.Failure("Failed to retrieve loyalty card");
        }
    }

    public async Task AddStampAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var settings = await _loyaltyRepository.GetSettingsAsync(ct);
            var stampsRequired = settings?.StampsRequired ?? DefaultStampsRequired;

            var card = await _loyaltyRepository.GetByUserIdAsync(userId, ct);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            int newStamps;
            LoyaltyCard updatedCard;

            if (card is null)
            {
                updatedCard = new LoyaltyCard
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Stamps = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _loyaltyRepository.CreateAsync(updatedCard, ct);
                newStamps = 1;
            }
            else
            {
                newStamps = card.Stamps + 1;
                updatedCard = new LoyaltyCard
                {
                    Id = card.Id,
                    UserId = card.UserId,
                    Stamps = newStamps >= stampsRequired ? newStamps - stampsRequired : newStamps,
                    CreatedAt = card.CreatedAt,
                    UpdatedAt = now
                };
                await _loyaltyRepository.UpdateAsync(updatedCard, ct);
            }

            if (newStamps >= stampsRequired)
            {
                var voucherCode = GenerateVoucherCode();
                var voucher = new LoyaltyVoucher
                {
                    Id = Guid.NewGuid(),
                    LoyaltyCardId = updatedCard.Id,
                    UserId = userId,
                    Code = voucherCode,
                    IsUsed = false,
                    IssuedAt = now,
                    UsedAt = null
                };
                await _loyaltyRepository.CreateVoucherAsync(voucher, ct);

                _logger.LogInformation(
                    "Free ticket voucher {VoucherCode} issued for user {UserId}",
                    voucherCode,
                    userId);
            }

            _logger.LogInformation(
                "Loyalty stamp added for user {UserId}. New stamp count: {Stamps}",
                userId,
                updatedCard.Stamps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding loyalty stamp for user {UserId}", userId);
        }
    }

    public async Task<Result<LoyaltySettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await _loyaltyRepository.GetSettingsAsync(ct);
            var stampsRequired = settings?.StampsRequired ?? DefaultStampsRequired;
            return Result<LoyaltySettingsDto>.Success(new LoyaltySettingsDto(stampsRequired));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty settings");
            return Result<LoyaltySettingsDto>.Failure("Failed to retrieve loyalty settings");
        }
    }

    public async Task<Result<LoyaltySettingsDto>> UpdateSettingsAsync(UpdateLoyaltySettingsDto dto, CancellationToken ct = default)
    {
        try
        {
            if (dto.StampsRequired < 1)
            {
                return Result<LoyaltySettingsDto>.Failure("Stamps required must be at least 1");
            }

            var existing = await _loyaltyRepository.GetSettingsAsync(ct);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var settings = new LoyaltySettings
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                StampsRequired = dto.StampsRequired,
                UpdatedAt = now
            };

            await _loyaltyRepository.UpsertSettingsAsync(settings, ct);

            return Result<LoyaltySettingsDto>.Success(new LoyaltySettingsDto(settings.StampsRequired));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loyalty settings");
            return Result<LoyaltySettingsDto>.Failure("Failed to update loyalty settings");
        }
    }

    private static string GenerateVoucherCode()
    {
        return $"FREE-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    }
}
