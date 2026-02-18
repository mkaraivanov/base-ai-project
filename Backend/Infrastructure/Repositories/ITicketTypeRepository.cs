using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ITicketTypeRepository
{
    Task<List<TicketType>> GetAllAsync(CancellationToken ct = default);
    Task<List<TicketType>> GetAllActiveAsync(CancellationToken ct = default);
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<TicketType>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<TicketType> CreateAsync(TicketType ticketType, CancellationToken ct = default);
    Task<TicketType> UpdateAsync(TicketType ticketType, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
