using System.Security.Claims;
using System.Text.Json;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

/// <summary>
/// EF Core interceptor that automatically captures every entity Add/Update/Delete
/// into an immutable AuditLog row within the same transaction.
///
/// For Updated entities the old values are sourced via two mechanisms:
/// 1. <see cref="IAuditCaptureService"/> — repositories register the entity's property
///    values *before* calling CurrentValues.SetValues(). This is the most reliable
///    approach because we read directly from the tracked entry while it still holds
///    the original DB snapshot.
/// 2. GetDatabaseValuesAsync() fallback — used for entities updated without going
///    through that pre-registration pattern (e.g. bare context.Update()).
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditCaptureService? _auditCapture;
    private readonly ILogger<AuditInterceptor> _logger;

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

    public AuditInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditInterceptor> logger,
        IAuditCaptureService? auditCapture = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _auditCapture = auditCapture;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            _pendingEntries = await CollectAuditEntriesAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
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
    private async Task<List<AuditEntry>> CollectAuditEntriesAsync(
        DbContext context, CancellationToken ct)
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

                var preCapture = _auditCapture?.GetPreUpdateValues(entry.Entity.GetType(), entityId);

                _logger.LogInformation(
                    "[AuditDiag] Processing Modified {EntityName} id={EntityId} — preCapture={Path}",
                    entityName, entityId,
                    preCapture is not null ? $"POPULATED ({preCapture.Count} keys)" : "NULL → using GetDatabaseValuesAsync fallback");

                if (preCapture is not null)
                {
                    _logger.LogInformation(
                        "[AuditDiag] preCapture keys: {Keys}",
                        string.Join(", ", preCapture.Keys));

                    foreach (var prop in entry.Properties)
                    {
                        if (SensitiveProperties.Contains(prop.Metadata.Name)) continue;

                        preCapture.TryGetValue(prop.Metadata.Name, out var oldVal);
                        var currentValue = prop.CurrentValue;
                        var areEqual = Equals(oldVal, currentValue);

                        _logger.LogInformation(
                            "[AuditDiag]   {Prop}: oldVal=({OldType}){OldVal}  currentValue=({CurType}){CurVal}  equal={Equal}",
                            prop.Metadata.Name,
                            oldVal?.GetType().Name ?? "null", oldVal ?? "(null)",
                            currentValue?.GetType().Name ?? "null", currentValue ?? "(null)",
                            areEqual);

                        if (!areEqual)
                        {
                            oldDict[prop.Metadata.Name] = oldVal;
                            newDict[prop.Metadata.Name] = currentValue;
                        }
                    }
                }
                else
                {
                    // Fallback: query the database for the current committed values.
                    // Works for direct context.Update() patterns where pre-registration
                    // is not available.
                    var dbValues = await entry.GetDatabaseValuesAsync(ct);

                    _logger.LogInformation(
                        "[AuditDiag] GetDatabaseValuesAsync returned {Result}",
                        dbValues is null ? "NULL" : "values");

                    foreach (var prop in entry.Properties)
                    {
                        if (SensitiveProperties.Contains(prop.Metadata.Name)) continue;

                        var dbValue = dbValues?[prop.Metadata];
                        var currentValue = prop.CurrentValue;
                        var areEqual = Equals(dbValue, currentValue);

                        _logger.LogInformation(
                            "[AuditDiag]   {Prop}: dbValue=({OldType}){OldVal}  currentValue=({CurType}){CurVal}  equal={Equal}",
                            prop.Metadata.Name,
                            dbValue?.GetType().Name ?? "null", dbValue ?? "(null)",
                            currentValue?.GetType().Name ?? "null", currentValue ?? "(null)",
                            areEqual);

                        if (!areEqual)
                        {
                            oldDict[prop.Metadata.Name] = dbValue;
                            newDict[prop.Metadata.Name] = currentValue;
                        }
                    }
                }

                _logger.LogInformation(
                    "[AuditDiag] Diff for {EntityName} id={EntityId}: {Count} changed propert(ies) — {Props}",
                    entityName, entityId, oldDict.Count,
                    oldDict.Count == 0 ? "(none)" : string.Join(", ", oldDict.Keys));

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
