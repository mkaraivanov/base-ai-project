using Application.DTOs.CinemaHalls;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class CinemaHallEndpoints
{
    public static RouteGroupBuilder MapCinemaHallEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetAllHallsAsync)
            .WithName("GetHalls")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetHallByIdAsync)
            .WithName("GetHall")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateHallAsync)
            .WithName("CreateHall")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:guid}", UpdateHallAsync)
            .WithName("UpdateHall")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteHallAsync)
            .WithName("DeleteHall")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetAllHallsAsync(
        ICinemaHallService hallService,
        bool? activeOnly,
        CancellationToken ct)
    {
        var result = await hallService.GetAllHallsAsync(activeOnly ?? true, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<CinemaHallDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<CinemaHallDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetHallByIdAsync(
        Guid id,
        ICinemaHallService hallService,
        CancellationToken ct)
    {
        var result = await hallService.GetHallByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<CinemaHallDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<CinemaHallDto>(false, null, result.Error));
    }

    private static async Task<IResult> CreateHallAsync(
        CreateCinemaHallDto dto,
        ICinemaHallService hallService,
        IValidator<CreateCinemaHallDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<CinemaHallDto>(false, null, "Validation failed", errors));
        }

        var result = await hallService.CreateHallAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/halls/{result.Value!.Id}", new ApiResponse<CinemaHallDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<CinemaHallDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateHallAsync(
        Guid id,
        UpdateCinemaHallDto dto,
        ICinemaHallService hallService,
        IValidator<UpdateCinemaHallDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<CinemaHallDto>(false, null, "Validation failed", errors));
        }

        var result = await hallService.UpdateHallAsync(id, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<CinemaHallDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<CinemaHallDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteHallAsync(
        Guid id,
        ICinemaHallService hallService,
        CancellationToken ct)
    {
        var result = await hallService.DeleteHallAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
