using Application.DTOs.TicketTypes;
using Application.Resources;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class TicketTypeService : ITicketTypeService
{
    private readonly ITicketTypeRepository _repository;
    private readonly ILogger<TicketTypeService> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TicketTypeService(
        ITicketTypeRepository repository,
        ILogger<TicketTypeService> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _repository = repository;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<List<TicketTypeDto>>> GetAllActiveAsync(CancellationToken ct = default)
    {
        try
        {
            var types = await _repository.GetAllActiveAsync(ct);
            return Result<List<TicketTypeDto>>.Success(types.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active ticket types");
            return Result<List<TicketTypeDto>>.Failure(_localizer["Failed to retrieve ticket types"]);
        }
    }

    public async Task<Result<List<TicketTypeDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var types = await _repository.GetAllAsync(ct);
            return Result<List<TicketTypeDto>>.Success(types.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all ticket types");
            return Result<List<TicketTypeDto>>.Failure(_localizer["Failed to retrieve ticket types"]);
        }
    }

    public async Task<Result<TicketTypeDto>> CreateAsync(CreateTicketTypeDto dto, CancellationToken ct = default)
    {
        try
        {
            if (dto.PriceModifier <= 0)
                return Result<TicketTypeDto>.Failure(_localizer["Price modifier must be greater than zero"]);

            var ticketType = new TicketType
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                PriceModifier = dto.PriceModifier,
                IsActive = true,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(ticketType, ct);
            _logger.LogInformation("Ticket type created: {Name} (modifier: {Modifier})", created.Name, created.PriceModifier);
            return Result<TicketTypeDto>.Success(ToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket type");
            return Result<TicketTypeDto>.Failure(_localizer["Failed to create ticket type"]);
        }
    }

    public async Task<Result<TicketTypeDto>> UpdateAsync(Guid id, UpdateTicketTypeDto dto, CancellationToken ct = default)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing is null)
                return Result<TicketTypeDto>.Failure(_localizer["Ticket type not found"]);

            if (dto.PriceModifier <= 0)
                return Result<TicketTypeDto>.Failure(_localizer["Price modifier must be greater than zero"]);

            var updated = existing with
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                PriceModifier = dto.PriceModifier,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };

            var saved = await _repository.UpdateAsync(updated, ct);
            _logger.LogInformation("Ticket type updated: {Id}", id);
            return Result<TicketTypeDto>.Success(ToDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket type {Id}", id);
            return Result<TicketTypeDto>.Failure(_localizer["Failed to update ticket type"]);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing is null)
                return Result.Failure(_localizer["Ticket type not found"]);

            await _repository.DeleteAsync(id, ct);
            _logger.LogInformation("Ticket type deleted (deactivated): {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket type {Id}", id);
            return Result.Failure(_localizer["Failed to delete ticket type"]);
        }
    }

    private static TicketTypeDto ToDto(TicketType t) =>
        new(t.Id, t.Name, t.Description, t.PriceModifier, t.IsActive, t.SortOrder);
}
