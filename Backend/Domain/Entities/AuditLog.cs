namespace Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Created | Updated | Deleted</summary>
    public string Action { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string? IpAddress { get; set; }

    /// <summary>JSON snapshot of old property values (for Updated/Deleted).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of new property values (for Created/Updated).</summary>
    public string? NewValues { get; set; }

    public DateTime Timestamp { get; set; }
}
