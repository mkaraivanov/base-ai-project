using Application.DTOs.Reporting;
using Application.Services;
using Backend.Infrastructure.Caching;
using Domain.Common;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class ReportingServiceTests
{
    private readonly Mock<IReportingRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<ReportingService>> _loggerMock;
    private readonly IReportingService _service;

    private static ReportQueryDto ValidQuery(string granularity = "Daily") =>
        new(
            From: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            Granularity: granularity);

    public ReportingServiceTests()
    {
        _repoMock = new Mock<IReportingRepository>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ReportingService>>();

        _service = new ReportingService(_repoMock.Object, _cacheMock.Object, _loggerMock.Object, Helpers.LocalizerHelper.CreateDefault());
    }

    // ── GetSalesByDateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesByDateAsync_WhenQueryIsValid_ReturnsDataFromRepository()
    {
        // Arrange
        var data = new List<SalesByDateDto>
        {
            new("2025-01-01", 10, 500m)
        };
        _repoMock.Setup(r => r.GetSalesByDateAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(data);
        _cacheMock.Setup(c => c.GetAsync<List<SalesByDateDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<SalesByDateDto>?)null);

        // Act
        var result = await _service.GetSalesByDateAsync(ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(data);
        _repoMock.Verify(r => r.GetSalesByDateAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSalesByDateAsync_WhenCacheHit_ReturnsCachedDataWithoutCallingRepository()
    {
        // Arrange
        var cached = new List<SalesByDateDto>
        {
            new("2025-01-01", 5, 250m)
        };
        _cacheMock.Setup(c => c.GetAsync<List<SalesByDateDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cached);

        // Act
        var result = await _service.GetSalesByDateAsync(ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cached);
        _repoMock.Verify(r => r.GetSalesByDateAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSalesByDateAsync_WhenCacheMiss_StoresResultInCache()
    {
        // Arrange
        var data = new List<SalesByDateDto> { new("2025-01-01", 3, 150m) };
        _cacheMock.Setup(c => c.GetAsync<List<SalesByDateDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<SalesByDateDto>?)null);
        _repoMock.Setup(r => r.GetSalesByDateAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(data);

        // Act
        await _service.GetSalesByDateAsync(ValidQuery());

        // Assert
        _cacheMock.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<SalesByDateDto>>(),
                It.Is<TimeSpan>(ts => ts == TimeSpan.FromMinutes(5)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSalesByDateAsync_WhenFromIsAfterTo_ReturnsFailure()
    {
        // Arrange
        var query = new ReportQueryDto(
            From: new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _service.GetSalesByDateAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("'from' date must be before 'to' date");
        _repoMock.Verify(r => r.GetSalesByDateAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSalesByDateAsync_WhenDateRangeExceeds366Days_ReturnsFailure()
    {
        // Arrange
        var query = new ReportQueryDto(
            From: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));   // > 366 days

        // Act
        var result = await _service.GetSalesByDateAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Date range cannot exceed 366 days");
    }

    [Theory]
    [InlineData("Hourly")]
    [InlineData("yearly")]
    [InlineData("")]
    public async Task GetSalesByDateAsync_WhenGranularityIsInvalid_ReturnsFailure(string granularity)
    {
        // Arrange
        var query = ValidQuery(granularity);

        // Act
        var result = await _service.GetSalesByDateAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid granularity");
    }

    // ── GetSalesByMovieAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesByMovieAsync_WhenQueryIsValid_ReturnsData()
    {
        // Arrange
        var data = new List<SalesByMovieDto>
        {
            new(Guid.NewGuid(), "Inception", 20, 1000m, 200, 0.8)
        };
        _cacheMock.Setup(c => c.GetAsync<List<SalesByMovieDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<SalesByMovieDto>?)null);
        _repoMock.Setup(r => r.GetSalesByMovieAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(data);

        // Act
        var result = await _service.GetSalesByMovieAsync(ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(data);
        _repoMock.Verify(r => r.GetSalesByMovieAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetSalesByShowtimeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesByShowtimeAsync_WhenQueryIsValid_ReturnsData()
    {
        // Arrange
        var data = new List<SalesByShowtimeDto>
        {
            new(Guid.NewGuid(), new DateTime(2025, 1, 10, 14, 0, 0, DateTimeKind.Utc),
                "Inception", "Hall A", "CineWorld", 15, 100, 0.15, 750m)
        };
        _cacheMock.Setup(c => c.GetAsync<List<SalesByShowtimeDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<SalesByShowtimeDto>?)null);
        _repoMock.Setup(r => r.GetSalesByShowtimeAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(data);

        // Act
        var result = await _service.GetSalesByShowtimeAsync(ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(data);
        _repoMock.Verify(r => r.GetSalesByShowtimeAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetSalesByLocationAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesByLocationAsync_WhenQueryIsValid_ReturnsData()
    {
        // Arrange
        var data = new List<SalesByLocationDto>
        {
            new(Guid.NewGuid(), "CineWorld", "London", "UK", 50, 2500m)
        };
        _cacheMock.Setup(c => c.GetAsync<List<SalesByLocationDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<SalesByLocationDto>?)null);
        _repoMock.Setup(r => r.GetSalesByLocationAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(data);

        // Act
        var result = await _service.GetSalesByLocationAsync(ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(data);
        _repoMock.Verify(r => r.GetSalesByLocationAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ExportCsvAsync ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("date")]
    [InlineData("movie")]
    [InlineData("location")]
    public async Task ExportCsvAsync_WhenReportTypeIsKnown_ReturnsCsvBytes(string reportType)
    {
        // Arrange
        var csvBytes = "col1,col2\nval1,val2"u8.ToArray();

        _repoMock.Setup(r => r.ExportSalesByDateCsvAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(csvBytes);
        _repoMock.Setup(r => r.ExportSalesByMovieCsvAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(csvBytes);
        _repoMock.Setup(r => r.ExportSalesByLocationCsvAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(csvBytes);

        // Act
        var result = await _service.ExportCsvAsync(reportType, ValidQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(csvBytes);
    }

    [Fact]
    public async Task ExportCsvAsync_WhenReportTypeIsUnknown_ReturnsFailure()
    {
        // Act
        var result = await _service.ExportCsvAsync("quarterly", ValidQuery());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unknown report type");
    }

    [Fact]
    public async Task ExportCsvAsync_WhenFromIsAfterTo_ReturnsFailure()
    {
        // Arrange
        var query = new ReportQueryDto(
            From: new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _service.ExportCsvAsync("date", query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("'from' date must be before 'to' date");
        _repoMock.Verify(r => r.ExportSalesByDateCsvAsync(It.IsAny<ReportQueryDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
