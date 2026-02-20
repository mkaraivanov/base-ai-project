using Application.DTOs.Cinemas;
using Application.Resources;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Backend.Endpoints;

public static class CinemaEndpoints
{
    public static RouteGroupBuilder MapCinemaEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetAllCinemasAsync)
            .WithName("GetCinemas")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetCinemaByIdAsync)
            .WithName("GetCinema")
            .AllowAnonymous();

        group.MapGet("/{id:guid}/halls", GetHallsByCinemaAsync)
            .WithName("GetHallsByCinema")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateCinemaAsync)
            .WithName("CreateCinema")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:guid}", UpdateCinemaAsync)
            .WithName("UpdateCinema")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteCinemaAsync)
            .WithName("DeleteCinema")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetAllCinemasAsync(
        ICinemaService cinemaService,
        bool? activeOnly,
        CancellationToken ct)
    {
        var result = await cinemaService.GetAllCinemasAsync(activeOnly ?? true, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<CinemaDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<CinemaDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetCinemaByIdAsync(
        Guid id,
        ICinemaService cinemaService,
        CancellationToken ct)
    {
        var result = await cinemaService.GetCinemaByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<CinemaDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<CinemaDto>(false, null, result.Error));
    }

    private static async Task<IResult> GetHallsByCinemaAsync(
        Guid id,
        ICinemaHallService hallService,
        CancellationToken ct)
    {
        var result = await hallService.GetAllHallsAsync(true, id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<Application.DTOs.CinemaHalls.CinemaHallDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<Application.DTOs.CinemaHalls.CinemaHallDto>>(false, null, result.Error));
    }

    private static async Task<IResult> CreateCinemaAsync(
        CreateCinemaDto dto,
        ICinemaService cinemaService,
        IValidator<CreateCinemaDto> validator,
        IStringLocalizer<SharedResource> localizer,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<CinemaDto>(false, null, localizer["Validation failed"], errors));
        }

        var result = await cinemaService.CreateCinemaAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/cinemas/{result.Value!.Id}", new ApiResponse<CinemaDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<CinemaDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateCinemaAsync(
        Guid id,
        UpdateCinemaDto dto,
        ICinemaService cinemaService,
        IValidator<UpdateCinemaDto> validator,
        IStringLocalizer<SharedResource> localizer,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<CinemaDto>(false, null, localizer["Validation failed"], errors));
        }

        var result = await cinemaService.UpdateCinemaAsync(id, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<CinemaDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<CinemaDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteCinemaAsync(
        Guid id,
        ICinemaService cinemaService,
        CancellationToken ct)
    {
        var result = await cinemaService.DeleteCinemaAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
