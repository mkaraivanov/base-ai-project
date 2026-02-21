using Application.DTOs.Audit;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly CinemaDbContext _context;

    public AuditLogRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (filter.DateFrom.HasValue)
            query = query.Where(l => l.Timestamp >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(l => l.Timestamp <= filter.DateTo.Value);

        if (filter.UserId.HasValue)
            query = query.Where(l => l.UserId == filter.UserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(l => l.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(l => l.EntityName == filter.EntityName);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(l =>
                l.UserEmail!.Contains(term) ||
                l.EntityName.Contains(term) ||
                l.EntityId.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }
}
