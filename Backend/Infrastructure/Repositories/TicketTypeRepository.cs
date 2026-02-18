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
        _context.TicketTypes.Update(ticketType);
        await _context.SaveChangesAsync(ct);
        return ticketType;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _context.TicketTypes.FindAsync([id], ct);
        if (existing is not null)
        {
            var deactivated = existing with { IsActive = false };
            _context.TicketTypes.Update(deactivated);
            await _context.SaveChangesAsync(ct);
        }
    }
}
