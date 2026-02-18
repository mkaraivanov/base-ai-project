using Application.DTOs.Loyalty;
using Application.Services;
using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class LoyaltyServiceTests
{
    private readonly Mock<ILoyaltyRepository> _loyaltyRepositoryMock;
    private readonly Mock<ILogger<LoyaltyService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILoyaltyService _loyaltyService;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime FixedNow = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public LoyaltyServiceTests()
    {
        _loyaltyRepositoryMock = new Mock<ILoyaltyRepository>();
        _loggerMock = new Mock<ILogger<LoyaltyService>>();
        _timeProvider = new FakeTimeProvider(FixedNow);

        _loyaltyService = new LoyaltyService(
            _loyaltyRepositoryMock.Object,
            _loggerMock.Object,
            _timeProvider
        );
    }

    [Fact]
    public async Task GetLoyaltyCardAsync_NoCardExists_ReturnsEmptyProgress()
    {
        // Arrange
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyCard?)null);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        // Act
        var result = await _loyaltyService.GetLoyaltyCardAsync(UserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Stamps);
        Assert.Equal(5, result.Value.StampsRequired);
        Assert.Equal(5, result.Value.StampsRemaining);
        Assert.Empty(result.Value.ActiveVouchers);
    }

    [Fact]
    public async Task GetLoyaltyCardAsync_CardWithStamps_ReturnsCorrectProgress()
    {
        // Arrange
        var card = new LoyaltyCard
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Stamps = 3,
            CreatedAt = FixedNow,
            UpdatedAt = FixedNow
        };
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltySettings { Id = Guid.NewGuid(), StampsRequired = 5, UpdatedAt = FixedNow });

        // Act
        var result = await _loyaltyService.GetLoyaltyCardAsync(UserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Stamps);
        Assert.Equal(5, result.Value.StampsRequired);
        Assert.Equal(2, result.Value.StampsRemaining);
    }

    [Fact]
    public async Task GetLoyaltyCardAsync_CardWithActiveVouchers_ReturnsOnlyActiveVouchers()
    {
        // Arrange
        var voucherId = Guid.NewGuid();
        var usedVoucherId = Guid.NewGuid();
        var card = new LoyaltyCard
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Stamps = 2,
            CreatedAt = FixedNow,
            UpdatedAt = FixedNow,
            Vouchers = new List<LoyaltyVoucher>
            {
                new LoyaltyVoucher { Id = voucherId, LoyaltyCardId = Guid.NewGuid(), UserId = UserId, Code = "FREE-ABC12345", IsUsed = false, IssuedAt = FixedNow },
                new LoyaltyVoucher { Id = usedVoucherId, LoyaltyCardId = Guid.NewGuid(), UserId = UserId, Code = "FREE-XYZ99999", IsUsed = true, IssuedAt = FixedNow, UsedAt = FixedNow }
            }
        };
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        // Act
        var result = await _loyaltyService.GetLoyaltyCardAsync(UserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.ActiveVouchers);
        Assert.Equal(voucherId, result.Value.ActiveVouchers[0].Id);
    }

    [Fact]
    public async Task AddStampAsync_FirstStamp_CreatesNewCard()
    {
        // Arrange
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyCard?)null);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        LoyaltyCard? createdCard = null;
        _loyaltyRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<LoyaltyCard>(), It.IsAny<CancellationToken>()))
            .Callback<LoyaltyCard, CancellationToken>((c, _) => createdCard = c)
            .ReturnsAsync((LoyaltyCard c, CancellationToken _) => c);

        // Act
        await _loyaltyService.AddStampAsync(UserId);

        // Assert
        _loyaltyRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<LoyaltyCard>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(createdCard);
        Assert.Equal(1, createdCard!.Stamps);
        Assert.Equal(UserId, createdCard.UserId);
    }

    [Fact]
    public async Task AddStampAsync_ExistingCard_IncrementsStamps()
    {
        // Arrange
        var existingCard = new LoyaltyCard
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Stamps = 2,
            CreatedAt = FixedNow,
            UpdatedAt = FixedNow
        };
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCard);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        LoyaltyCard? updatedCard = null;
        _loyaltyRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<LoyaltyCard>(), It.IsAny<CancellationToken>()))
            .Callback<LoyaltyCard, CancellationToken>((c, _) => updatedCard = c)
            .ReturnsAsync((LoyaltyCard c, CancellationToken _) => c);

        // Act
        await _loyaltyService.AddStampAsync(UserId);

        // Assert
        _loyaltyRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<LoyaltyCard>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(updatedCard);
        Assert.Equal(3, updatedCard!.Stamps);
    }

    [Fact]
    public async Task AddStampAsync_ReachesThreshold_CreatesVoucherAndResetsStamps()
    {
        // Arrange
        var existingCard = new LoyaltyCard
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Stamps = 4, // one more stamp will reach threshold of 5
            CreatedAt = FixedNow,
            UpdatedAt = FixedNow
        };
        _loyaltyRepositoryMock
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCard);
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        LoyaltyCard? updatedCard = null;
        _loyaltyRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<LoyaltyCard>(), It.IsAny<CancellationToken>()))
            .Callback<LoyaltyCard, CancellationToken>((c, _) => updatedCard = c)
            .ReturnsAsync((LoyaltyCard c, CancellationToken _) => c);

        LoyaltyVoucher? createdVoucher = null;
        _loyaltyRepositoryMock
            .Setup(x => x.CreateVoucherAsync(It.IsAny<LoyaltyVoucher>(), It.IsAny<CancellationToken>()))
            .Callback<LoyaltyVoucher, CancellationToken>((v, _) => createdVoucher = v)
            .ReturnsAsync((LoyaltyVoucher v, CancellationToken _) => v);

        // Act
        await _loyaltyService.AddStampAsync(UserId);

        // Assert
        Assert.NotNull(updatedCard);
        Assert.Equal(0, updatedCard!.Stamps); // Reset after reaching 5
        _loyaltyRepositoryMock.Verify(x => x.CreateVoucherAsync(It.IsAny<LoyaltyVoucher>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(createdVoucher);
        Assert.False(createdVoucher!.IsUsed);
        Assert.StartsWith("FREE-", createdVoucher.Code);
    }

    [Fact]
    public async Task GetSettingsAsync_NoSettingsExist_ReturnsDefault()
    {
        // Arrange
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);

        // Act
        var result = await _loyaltyService.GetSettingsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.StampsRequired);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ValidValue_UpdatesSettings()
    {
        // Arrange
        _loyaltyRepositoryMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings?)null);
        _loyaltyRepositoryMock
            .Setup(x => x.UpsertSettingsAsync(It.IsAny<LoyaltySettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltySettings s, CancellationToken _) => s);

        // Act
        var result = await _loyaltyService.UpdateSettingsAsync(new UpdateLoyaltySettingsDto(10));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.StampsRequired);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ZeroValue_ReturnsFailure()
    {
        // Act
        var result = await _loyaltyService.UpdateSettingsAsync(new UpdateLoyaltySettingsDto(0));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }
}
