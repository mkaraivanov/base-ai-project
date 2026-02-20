using Application.DTOs.Auth;
using Application.Resources;
using Application.Services;
using Application.Validators;
using Backend.Models;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Backend.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new customer account");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Login user")
            .WithDescription("Authenticates user and returns JWT token");

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto dto,
        IAuthService authService,
        IValidator<RegisterDto> validator,
        IStringLocalizer<SharedResource> localizer,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, localizer["Validation failed"], errors));
        }

        // Register user
        var result = await authService.RegisterAsync(dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<AuthResponseDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, result.Error));
    }

    private static async Task<IResult> LoginAsync(
        LoginDto dto,
        IAuthService authService,
        IValidator<LoginDto> validator,
        IStringLocalizer<SharedResource> localizer,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<AuthResponseDto>(false, null, localizer["Validation failed"], errors));
        }

        // Login user
        var result = await authService.LoginAsync(dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<AuthResponseDto>(true, result.Value, null))
            : Results.Unauthorized();
    }
}
