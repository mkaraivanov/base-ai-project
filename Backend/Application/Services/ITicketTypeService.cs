using Application.DTOs.TicketTypes;
using Domain.Common;

namespace Application.Services;

public interface ITicketTypeService
{
    Task<Result<List<TicketTypeDto>>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Result<List<TicketTypeDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<TicketTypeDto>> CreateAsync(CreateTicketTypeDto dto, CancellationToken ct = default);
    Task<Result<TicketTypeDto>> UpdateAsync(Guid id, UpdateTicketTypeDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
