using System.Security.Claims;
using Application.DTOs.Loyalty;
using Application.Services;
using Backend.Models;

namespace Backend.Endpoints;

public static class LoyaltyEndpoints
{
    public static RouteGroupBuilder MapLoyaltyEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/my-card", GetMyLoyaltyCardAsync)
            .WithName("GetMyLoyaltyCard")
            .RequireAuthorization();

        group.MapGet("/settings", GetSettingsAsync)
            .WithName("GetLoyaltySettings")
            .AllowAnonymous();

        group.MapPut("/settings", UpdateSettingsAsync)
            .WithName("UpdateLoyaltySettings")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetMyLoyaltyCardAsync(
        ILoyaltyService loyaltyService,
        HttpContext context,
        CancellationToken ct)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await loyaltyService.GetLoyaltyCardAsync(userId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<LoyaltyCardDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<LoyaltyCardDto>(false, null, result.Error));
    }

    private static async Task<IResult> GetSettingsAsync(
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        var result = await loyaltyService.GetSettingsAsync(ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<LoyaltySettingsDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<LoyaltySettingsDto>(false, null, result.Error));
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateLoyaltySettingsDto dto,
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        if (dto.StampsRequired < 1)
        {
            return Results.BadRequest(
                new ApiResponse<LoyaltySettingsDto>(false, null, "Stamps required must be at least 1"));
        }

        var result = await loyaltyService.UpdateSettingsAsync(dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<LoyaltySettingsDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<LoyaltySettingsDto>(false, null, result.Error));
    }
}
