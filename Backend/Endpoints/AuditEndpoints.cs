using Application.DTOs.Audit;
using Application.Services;
using Backend.Models;

namespace Backend.Endpoints;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetLogsAsync)
            .WithName("GetAuditLogs")
            .RequireAuthorization("Admin");

        group.MapGet("/{id:guid}", GetLogByIdAsync)
            .WithName("GetAuditLog")
            .RequireAuthorization("Admin");

        group.MapGet("/export", ExportCsvAsync)
            .WithName("ExportAuditLogs")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetLogsAsync(
        IAuditLogService auditService,
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? userId,
        string? action,
        string? entityName,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var filter = new AuditLogFilterDto(dateFrom, dateTo, userId, action, entityName, search);
        var result = await auditService.GetLogsAsync(filter, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<PagedResult<AuditLogDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<PagedResult<AuditLogDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetLogByIdAsync(
        Guid id,
        IAuditLogService auditService,
        CancellationToken ct)
    {
        var result = await auditService.GetLogByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<AuditLogDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<AuditLogDto>(false, null, result.Error));
    }

    private static async Task<IResult> ExportCsvAsync(
        IAuditLogService auditService,
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? userId,
        string? action,
        string? entityName,
        string? search,
        CancellationToken ct)
    {
        var filter = new AuditLogFilterDto(dateFrom, dateTo, userId, action, entityName, search);
        var result = await auditService.ExportCsvAsync(filter, ct);

        if (!result.IsSuccess)
            return Results.BadRequest(new ApiResponse<object>(false, null, result.Error));

        var filename = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return Results.File(result.Value!, "text/csv", filename);
    }
}
