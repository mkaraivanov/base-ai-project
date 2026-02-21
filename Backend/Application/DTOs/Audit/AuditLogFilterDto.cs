namespace Application.DTOs.Audit;

public record AuditLogFilterDto(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? UserId = null,
    string? Action = null,
    string? EntityName = null,
    string? SearchTerm = null
);
