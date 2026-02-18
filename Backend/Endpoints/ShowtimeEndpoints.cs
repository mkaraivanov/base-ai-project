using Application.DTOs.Showtimes;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class ShowtimeEndpoints
{
    public static RouteGroupBuilder MapShowtimeEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetShowtimesAsync)
            .WithName("GetShowtimes")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetShowtimeByIdAsync)
            .WithName("GetShowtime")
            .AllowAnonymous();

        group.MapGet("/movie/{movieId:guid}", GetShowtimesByMovieAsync)
            .WithName("GetShowtimesByMovie")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateShowtimeAsync)
            .WithName("CreateShowtime")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteShowtimeAsync)
            .WithName("DeleteShowtime")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetShowtimesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? cinemaId,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimesAsync(fromDate, toDate, cinemaId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<ShowtimeDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<ShowtimeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetShowtimeByIdAsync(
        Guid id,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimeByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<ShowtimeDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<ShowtimeDto>(false, null, result.Error));
    }

    private static async Task<IResult> GetShowtimesByMovieAsync(
        Guid movieId,
        Guid? cinemaId,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.GetShowtimesByMovieAsync(movieId, cinemaId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<ShowtimeDto>>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<List<ShowtimeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> CreateShowtimeAsync(
        CreateShowtimeDto dto,
        IShowtimeService showtimeService,
        IValidator<CreateShowtimeDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<ShowtimeDto>(false, null, "Validation failed", errors));
        }

        var result = await showtimeService.CreateShowtimeAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/showtimes/{result.Value!.Id}", new ApiResponse<ShowtimeDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<ShowtimeDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteShowtimeAsync(
        Guid id,
        IShowtimeService showtimeService,
        CancellationToken ct)
    {
        var result = await showtimeService.DeleteShowtimeAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
