using Application.DTOs.Audit;
using Domain.Common;

namespace Application.Services;

public interface IAuditLogService
{
    Task<Result<PagedResult<AuditLogDto>>> GetLogsAsync(
        AuditLogFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<AuditLogDto>> GetLogByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<byte[]>> ExportCsvAsync(AuditLogFilterDto filter, CancellationToken ct = default);
}
