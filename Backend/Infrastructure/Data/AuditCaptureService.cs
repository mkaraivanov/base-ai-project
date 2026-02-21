namespace Infrastructure.Data;

/// <inheritdoc />
public sealed class AuditCaptureService : IAuditCaptureService
{
    // Keyed by (entity CLR type, primary-key string) so multiple entities of the
    // same type can be updated in a single request without collisions.
    private readonly Dictionary<(Type, string), IReadOnlyDictionary<string, object?>> _store = [];

    /// <inheritdoc />
    public void RegisterPreUpdateValues(
        Type entityType,
        string entityId,
        IReadOnlyDictionary<string, object?> oldValues)
    {
        _store[(entityType, entityId)] = oldValues;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? GetPreUpdateValues(Type entityType, string entityId)
    {
        return _store.TryGetValue((entityType, entityId), out var values) ? values : null;
    }
}
