namespace Application.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    Guid? UserId,
    string? UserEmail,
    string? UserRole,
    string? IpAddress,
    string? OldValues,
    string? NewValues,
    DateTime Timestamp
);
