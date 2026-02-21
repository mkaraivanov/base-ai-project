using Application.DTOs.Audit;
using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
