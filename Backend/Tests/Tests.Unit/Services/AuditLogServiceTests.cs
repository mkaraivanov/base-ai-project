using Application.DTOs.Audit;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Unit.Helpers;

namespace Tests.Unit.Services;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repositoryMock = new();
    private readonly Mock<ILogger<AuditLogService>> _loggerMock = new();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(
            _repositoryMock.Object,
            _loggerMock.Object,
            LocalizerHelper.CreateDefault());
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static AuditLog MakeLog(
        string entityName = "Movie",
        string action = "Created",
        string? oldValues = null,
        string? newValues = "{\"Title\":\"Test\"}") => new()
    {
        Id = Guid.NewGuid(),
        EntityName = entityName,
        EntityId = Guid.NewGuid().ToString(),
        Action = action,
        UserId = Guid.NewGuid(),
        UserEmail = "admin@test.com",
        UserRole = "Admin",
        IpAddress = "127.0.0.1",
        OldValues = oldValues,
        NewValues = newValues,
        Timestamp = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc)
    };

    private void SetupRepository(List<AuditLog> logs, int total, int page, int pageSize)
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, total));
    }

    // ── GetLogsAsync – happy path ─────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_WithValidFilter_ReturnsMappedDtos()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            MakeLog("Movie", "Created"),
            MakeLog("Cinema", "Updated", oldValues: "{\"Name\":\"Old\"}"),
        };
        SetupRepository(logs, total: 2, page: 1, pageSize: 20);

        // Act
        var result = await _sut.GetLogsAsync(new AuditLogFilterDto(), page: 1, pageSize: 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);

        result.Value.Items[0].EntityName.Should().Be("Movie");
        result.Value.Items[0].Action.Should().Be("Created");
        result.Value.Items[1].EntityName.Should().Be("Cinema");
        result.Value.Items[1].OldValues.Should().Contain("Old");
    }

    [Fact]
    public async Task GetLogsAsync_WithEmptyResult_ReturnsEmptyPage()
    {
        // Arrange
        SetupRepository([], total: 0, page: 1, pageSize: 10);

        // Act
        var result = await _sut.GetLogsAsync(new AuditLogFilterDto(), page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    // ── GetLogsAsync – page / pageSize normalisation ──────────────────────────

    [Theory]
    [InlineData(0, 20, 1, 20)]    // page below minimum → clamped to 1
    [InlineData(-5, 50, 1, 50)]   // negative page → clamped to 1
    [InlineData(1, 0, 1, 1)]      // pageSize < 1 → clamped to 1
    [InlineData(1, 200, 1, 100)]  // pageSize > 100 → clamped to 100
    [InlineData(3, 100, 3, 100)]  // valid values passed through unchanged
    public async Task GetLogsAsync_NormalizesPageAndPageSize(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        SetupRepository([], total: 0, page: expectedPage, pageSize: expectedPageSize);

        // Act
        var result = await _sut.GetLogsAsync(new AuditLogFilterDto(), page, pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(expectedPage);
        result.Value.PageSize.Should().Be(expectedPageSize);

        _repositoryMock.Verify(
            r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), expectedPage, expectedPageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetLogsAsync – error path ─────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _sut.GetLogsAsync(new AuditLogFilterDto(), page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ── GetLogByIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogByIdAsync_WhenFound_ReturnsMappedDto()
    {
        // Arrange
        var log = MakeLog();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(log.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        var result = await _sut.GetLogByIdAsync(log.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(log.Id);
        result.Value.EntityName.Should().Be(log.EntityName);
        result.Value.Action.Should().Be(log.Action);
        result.Value.UserEmail.Should().Be(log.UserEmail);
        result.Value.UserRole.Should().Be(log.UserRole);
        result.Value.IpAddress.Should().Be(log.IpAddress);
        result.Value.Timestamp.Should().Be(log.Timestamp);
    }

    [Fact]
    public async Task GetLogByIdAsync_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);

        // Act
        var result = await _sut.GetLogByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLogByIdAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _sut.GetLogByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ── ExportCsvAsync – happy path ───────────────────────────────────────────

    [Fact]
    public async Task ExportCsvAsync_ReturnsCsvWithHeaderRowAndDataRow()
    {
        // Arrange
        var log = MakeLog();
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), 1, 100_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([log], 1));

        // Act
        var result = await _sut.ExportCsvAsync(new AuditLogFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csv = System.Text.Encoding.UTF8.GetString(result.Value!);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[0].Should().Contain("Id")
            .And.Contain("Timestamp")
            .And.Contain("EntityName")
            .And.Contain("Action")
            .And.Contain("UserEmail");
        lines.Should().HaveCountGreaterThanOrEqualTo(2);
        lines[1].Should().Contain(log.Id.ToString());
        lines[1].Should().Contain(log.EntityName);
        lines[1].Should().Contain(log.Action);
    }

    [Fact]
    public async Task ExportCsvAsync_WithEmptyResult_ReturnsCsvWithHeaderOnly()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), 1, 100_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0));

        // Act
        var result = await _sut.ExportCsvAsync(new AuditLogFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csv = System.Text.Encoding.UTF8.GetString(result.Value!);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1); // header only
    }

    [Fact]
    public async Task ExportCsvAsync_EscapesDoubleQuotesInFieldValues()
    {
        // Arrange – a value containing double-quotes must be wrapped and escaped
        var log = MakeLog();
        log.NewValues = "has \"quotes\" inside";
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), 1, 100_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([log], 1));

        // Act
        var result = await _sut.ExportCsvAsync(new AuditLogFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csv = System.Text.Encoding.UTF8.GetString(result.Value!);
        // RFC 4180: double-quote is escaped by doubling it
        csv.Should().Contain("\"\"quotes\"\"");
    }

    [Fact]
    public async Task ExportCsvAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _sut.ExportCsvAsync(new AuditLogFilterDto());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportCsvAsync_RequestsUpToMaxExportRowsFromRepository()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0));

        // Act
        await _sut.ExportCsvAsync(new AuditLogFilterDto());

        // Assert – 100 000 is the MaxExportRows constant
        _repositoryMock.Verify(
            r => r.GetPagedAsync(
                It.IsAny<AuditLogFilterDto>(), 1, 100_000, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
