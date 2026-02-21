using System.Security.Claims;
using System.Text.Json;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data;

/// <summary>
/// EF Core interceptor that automatically captures every entity Add/Update/Delete
/// into an immutable AuditLog row within the same transaction.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly HashSet<string> SensitiveProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "Password",
            "Secret",
            "Token",
            "RefreshToken"
        };

    // Entities that should never generate audit entries (prevents recursion).
    private static readonly HashSet<Type> ExcludedTypes = new() { typeof(AuditLog) };

    private List<AuditEntry> _pendingEntries = [];

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            _pendingEntries = CollectAuditEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingEntries.Count > 0 && eventData.Context is not null)
        {
            var timestamp = DateTime.UtcNow;
            var logs = _pendingEntries.Select(e => e.ToAuditLog(timestamp)).ToList();
            _pendingEntries = [];

            eventData.Context.Set<AuditLog>().AddRange(logs);
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // -------------------------------------------------------------------------
    private List<AuditEntry> CollectAuditEntries(DbContext context)
    {
        var userId = GetUserId();
        var userEmail = GetUserEmail();
        var userRole = GetUserRole();
        var ipAddress = GetIpAddress();

        var entries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (ExcludedTypes.Contains(entry.Entity.GetType())) continue;

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ => "Updated"
            };

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified)
            {
                var oldDict = new Dictionary<string, object?>();
                var newDict = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (SensitiveProperties.Contains(prop.Metadata.Name)) continue;
                    if (!prop.IsModified) continue;
                    oldDict[prop.Metadata.Name] = prop.OriginalValue;
                    newDict[prop.Metadata.Name] = prop.CurrentValue;
                }
                if (oldDict.Count > 0)
                {
                    oldValues = JsonSerializer.Serialize(oldDict);
                    newValues = JsonSerializer.Serialize(newDict);
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var dict = entry.Properties
                    .Where(p => !SensitiveProperties.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                newValues = JsonSerializer.Serialize(dict);
            }
            else // Deleted
            {
                var dict = entry.Properties
                    .Where(p => !SensitiveProperties.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                oldValues = JsonSerializer.Serialize(dict);
            }

            entries.Add(new AuditEntry(
                entityName,
                entityId,
                action,
                userId,
                userEmail,
                userRole,
                ipAddress,
                oldValues,
                newValues));
        }

        return entries;
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keys = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty)
            .ToList();
        return keys is not null ? string.Join(",", keys) : string.Empty;
    }

    private Guid? GetUserId()
    {
        var raw = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private string? GetUserEmail() =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

    private string? GetUserRole() =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    private string? GetIpAddress() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

internal record AuditEntry(
    string EntityName,
    string EntityId,
    string Action,
    Guid? UserId,
    string? UserEmail,
    string? UserRole,
    string? IpAddress,
    string? OldValues,
    string? NewValues)
{
    public AuditLog ToAuditLog(DateTime timestamp) => new()
    {
        Id = Guid.NewGuid(),
        EntityName = EntityName,
        EntityId = EntityId,
        Action = Action,
        UserId = UserId,
        UserEmail = UserEmail,
        UserRole = UserRole,
        IpAddress = IpAddress,
        OldValues = OldValues,
        NewValues = NewValues,
        Timestamp = timestamp
    };
}
