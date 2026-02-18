using Application.DTOs.TicketTypes;
using Application.Services;
using Backend.Models;

namespace Backend.Endpoints;

public static class TicketTypeEndpoints
{
    public static RouteGroupBuilder MapTicketTypeEndpoints(this RouteGroupBuilder group)
    {
        // Public: active types only (used by customer seat-selection)
        group.MapGet("/", GetActiveTicketTypesAsync)
            .WithName("GetTicketTypes")
            .AllowAnonymous();

        // Admin: all types including inactive
        group.MapGet("/all", GetAllTicketTypesAsync)
            .WithName("GetAllTicketTypes")
            .RequireAuthorization("Admin");

        group.MapPost("/", CreateTicketTypeAsync)
            .WithName("CreateTicketType")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:guid}", UpdateTicketTypeAsync)
            .WithName("UpdateTicketType")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteTicketTypeAsync)
            .WithName("DeleteTicketType")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetActiveTicketTypesAsync(
        ITicketTypeService service,
        CancellationToken ct)
    {
        var result = await service.GetAllActiveAsync(ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<TicketTypeDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<TicketTypeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetAllTicketTypesAsync(
        ITicketTypeService service,
        CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<TicketTypeDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<TicketTypeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> CreateTicketTypeAsync(
        CreateTicketTypeDto dto,
        ITicketTypeService service,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(dto, ct);
        return result.IsSuccess
            ? Results.Created($"/api/ticket-types/{result.Value!.Id}", new ApiResponse<TicketTypeDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<TicketTypeDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateTicketTypeAsync(
        Guid id,
        UpdateTicketTypeDto dto,
        ITicketTypeService service,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, dto, ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<TicketTypeDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<TicketTypeDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteTicketTypeAsync(
        Guid id,
        ITicketTypeService service,
        CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
