using System.Text;
using Application.DTOs.Audit;
using Application.Resources;
using Application.Services;
using Domain.Common;
using Infrastructure.Repositories;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private const int MaxExportRows = 100_000;

    public AuditLogService(
        IAuditLogRepository repository,
        ILogger<AuditLogService> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _repository = repository;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> GetLogsAsync(
        AuditLogFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, totalCount) = await _repository.GetPagedAsync(filter, page, pageSize, ct);
            var dtos = items.Select(MapToDto).ToList();

            return Result<PagedResult<AuditLogDto>>.Success(
                new PagedResult<AuditLogDto>(dtos, totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return Result<PagedResult<AuditLogDto>>.Failure(_localizer["Failed to retrieve audit logs"]);
        }
    }

    public async Task<Result<AuditLogDto>> GetLogByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var log = await _repository.GetByIdAsync(id, ct);
            if (log is null)
                return Result<AuditLogDto>.Failure(_localizer["Audit log not found"]);

            return Result<AuditLogDto>.Success(MapToDto(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {AuditLogId}", id);
            return Result<AuditLogDto>.Failure(_localizer["Failed to retrieve audit log"]);
        }
    }

    public async Task<Result<byte[]>> ExportCsvAsync(AuditLogFilterDto filter, CancellationToken ct = default)
    {
        try
        {
            var (items, _) = await _repository.GetPagedAsync(filter, page: 1, pageSize: MaxExportRows, ct);

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,EntityName,EntityId,Action,UserId,UserEmail,UserRole,IpAddress,OldValues,NewValues");

            foreach (var log in items)
            {
                csv.AppendLine(string.Join(',',
                    Escape(log.Id.ToString()),
                    Escape(log.Timestamp.ToString("o")),
                    Escape(log.EntityName),
                    Escape(log.EntityId),
                    Escape(log.Action),
                    Escape(log.UserId?.ToString() ?? string.Empty),
                    Escape(log.UserEmail ?? string.Empty),
                    Escape(log.UserRole ?? string.Empty),
                    Escape(log.IpAddress ?? string.Empty),
                    Escape(log.OldValues ?? string.Empty),
                    Escape(log.NewValues ?? string.Empty)));
            }

            return Result<byte[]>.Success(Encoding.UTF8.GetBytes(csv.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs to CSV");
            return Result<byte[]>.Failure(_localizer["Failed to export audit logs"]);
        }
    }

    private static AuditLogDto MapToDto(Domain.Entities.AuditLog log) => new(
        log.Id,
        log.EntityName,
        log.EntityId,
        log.Action,
        log.UserId,
        log.UserEmail,
        log.UserRole,
        log.IpAddress,
        log.OldValues,
        log.NewValues,
        log.Timestamp);

    /// <summary>Wraps a CSV field in double-quotes and escapes any embedded quotes.</summary>
    private static string Escape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
