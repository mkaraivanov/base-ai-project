using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TicketTypeRepository : ITicketTypeRepository
{
    private readonly CinemaDbContext _context;

    public TicketTypeRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<List<TicketType>> GetAllAsync(CancellationToken ct = default)
        => await _context.TicketTypes
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<List<TicketType>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.TicketTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TicketTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<TicketType>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => await _context.TicketTypes
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct);

    public async Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default)
    {
        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync(ct);
        return ticketType;
    }

    public async Task<TicketType> UpdateAsync(TicketType ticketType, CancellationToken ct = default)
    {
        // Load the tracked entity first so EF Core preserves the original-value snapshot.
        // Without this, calling Update() on a detached record sets OriginalValues = CurrentValues,
        // making the audit log show identical old and new values.
        var existing = await _context.TicketTypes.FindAsync([ticketType.Id], ct);
        if (existing is null)
            return ticketType;

        _context.Entry(existing).CurrentValues.SetValues(ticketType);
        await _context.SaveChangesAsync(ct);
        return ticketType;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _context.TicketTypes.FindAsync([id], ct);
        if (existing is not null)
        {
            // Use the property API so EF Core retains the original IsActive value in the
            // change-tracker snapshot, giving the audit log a correct before/after diff.
            _context.Entry(existing).Property(t => t.IsActive).CurrentValue = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
