using System.Security.Claims;
using Application.DTOs.Bookings;
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Application.Services;
using Backend.Models;
using FluentValidation;

namespace Backend.Endpoints;

public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoint
        group.MapGet("/availability/{showtimeId:guid}", GetSeatAvailabilityAsync)
            .WithName("GetSeatAvailability")
            .AllowAnonymous();

        // Authenticated endpoints
        group.MapPost("/reserve", CreateReservationAsync)
            .WithName("CreateReservation")
            .RequireAuthorization();

        group.MapDelete("/reserve/{reservationId:guid}", CancelReservationAsync)
            .WithName("CancelReservation")
            .RequireAuthorization();

        group.MapPost("/confirm", ConfirmBookingAsync)
            .WithName("ConfirmBooking")
            .RequireAuthorization();

        group.MapGet("/my-bookings", GetMyBookingsAsync)
            .WithName("GetMyBookings")
            .RequireAuthorization();

        group.MapGet("/{bookingNumber}", GetBookingByNumberAsync)
            .WithName("GetBookingByNumber")
            .RequireAuthorization();

        group.MapPost("/{bookingId:guid}/cancel", CancelBookingAsync)
            .WithName("CancelBooking")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetSeatAvailabilityAsync(
        Guid showtimeId,
        IBookingService bookingService,
        CancellationToken ct)
    {
        var result = await bookingService.GetSeatAvailabilityAsync(showtimeId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<SeatAvailabilityDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<SeatAvailabilityDto>(false, null, result.Error));
    }

    private static async Task<IResult> CreateReservationAsync(
        CreateReservationDto dto,
        IBookingService bookingService,
        IValidator<CreateReservationDto> validator,
        HttpContext context,
        CancellationToken ct)
    {
        // Verify user is authenticated - check both Identity and Authorization header
        var hasAuthHeader = context.Request.Headers.ContainsKey("Authorization");
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        if (!hasAuthHeader || !isAuthenticated)
        {
            return Results.Unauthorized();
        }

        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<ReservationDto>(false, null, "Validation failed", errors));
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        // Create reservation
        var result = await bookingService.CreateReservationAsync(userId, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<ReservationDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<ReservationDto>(false, null, result.Error));
    }

    private static async Task<IResult> CancelReservationAsync(
        Guid reservationId,
        IBookingService bookingService,
        HttpContext context,
        CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.CancelReservationAsync(userId, reservationId, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ApiResponse<object>(false, null, result.Error));
    }

    private static async Task<IResult> ConfirmBookingAsync(
        ConfirmBookingDto dto,
        IBookingService bookingService,
        IValidator<ConfirmBookingDto> validator,
        HttpContext context,
        CancellationToken ct)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new ApiResponse<BookingDto>(false, null, "Validation failed", errors));
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.ConfirmBookingAsync(userId, dto, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<BookingDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<BookingDto>(false, null, result.Error));
    }

    private static async Task<IResult> GetMyBookingsAsync(
        IBookingService bookingService,
        HttpContext context,
        CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.GetMyBookingsAsync(userId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<BookingDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<BookingDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetBookingByNumberAsync(
        string bookingNumber,
        IBookingService bookingService,
        HttpContext context,
        CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.GetBookingByNumberAsync(userId, bookingNumber, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<BookingDto>(true, result.Value, null))
            : Results.NotFound(new ApiResponse<BookingDto>(false, null, result.Error));
    }

    private static async Task<IResult> CancelBookingAsync(
        Guid bookingId,
        IBookingService bookingService,
        HttpContext context,
        CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await bookingService.CancelBookingAsync(userId, bookingId, ct);

        return result.IsSuccess
            ? Results.Ok(new ApiResponse<BookingDto>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<BookingDto>(false, null, result.Error));
    }
}
