using Application.DTOs.Movies;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class MovieEndpoints
{
    public static RouteGroupBuilder MapMovieEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoints
        group.MapGet("/", GetAllMoviesAsync)
            .WithName("GetMovies")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetMovieByIdAsync)
            .WithName("GetMovie")
            .AllowAnonymous();

        // Admin endpoints
        group.MapPost("/", CreateMovieAsync)
            .WithName("CreateMovie")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:guid}", UpdateMovieAsync)
            .WithName("UpdateMovie")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:guid}", DeleteMovieAsync)
            .WithName("DeleteMovie")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetAllMoviesAsync(
        IMovieService movieService,
        bool? activeOnly,
        CancellationToken ct)
    {
        var result = await movieService.GetAllMoviesAsync(activeOnly ?? true, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<MovieDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<MovieDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetMovieByIdAsync(
        Guid id,
        IMovieService movieService,
        CancellationToken ct)
    {
        var result = await movieService.GetMovieByIdAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> CreateMovieAsync(
        CreateMovieDto dto,
        IMovieService movieService,
        IValidator<CreateMovieDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<MovieDto>(false, null, "Validation failed", errors));
        }

        var result = await movieService.CreateMovieAsync(dto, ct);

        return result.IsSuccess
            ? Results.Created($"/api/movies/{result.Value!.Id}", new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateMovieAsync(
        Guid id,
        UpdateMovieDto dto,
        IMovieService movieService,
        IValidator<UpdateMovieDto> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<MovieDto>(false, null, "Validation failed", errors));
        }

        var result = await movieService.UpdateMovieAsync(id, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<MovieDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<MovieDto>(false, null, result.Error));
    }

    private static async Task<IResult> DeleteMovieAsync(
        Guid id,
        IMovieService movieService,
        CancellationToken ct)
    {
        var result = await movieService.DeleteMovieAsync(id, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }
}
