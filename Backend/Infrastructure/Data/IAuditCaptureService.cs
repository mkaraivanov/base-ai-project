namespace Infrastructure.Data;

/// <summary>
/// Scoped service that allows repositories to pre-register the old (pre-change)
/// entity property values before calling CurrentValues.SetValues().
///
/// This is necessary because EF Core's change-tracker snapshot for record entities
/// with init-only properties can be unreliable after SetValues() is called —
/// both OriginalValue and GetDatabaseValuesAsync() may return the new data
/// on SQL Server. Capturing values from the tracked entry's CurrentValue
/// *before* SetValues() guarantees the correct before-state.
/// </summary>
public interface IAuditCaptureService
{
    /// <summary>
    /// Stores the old property values for an entity that is about to be updated.
    /// Call this after FindAsync() but before CurrentValues.SetValues().
    /// </summary>
    void RegisterPreUpdateValues(
        Type entityType,
        string entityId,
        IReadOnlyDictionary<string, object?> oldValues);

    /// <summary>
    /// Retrieves previously registered old values for the given entity,
    /// or <c>null</c> if none were registered.
    /// </summary>
    IReadOnlyDictionary<string, object?>? GetPreUpdateValues(Type entityType, string entityId);
}
