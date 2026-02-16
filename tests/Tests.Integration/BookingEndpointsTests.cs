using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Backend.Models;
using FluentAssertions;
using Tests.Integration.Helpers;
using Xunit;

namespace Tests.Integration;

public class BookingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSeatAvailability_ValidShowtime_ReturnsSeats()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, _) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        // Act
        var response = await client.GetAsync($"/api/bookings/availability/{showtimeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeatAvailabilityDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalSeats.Should().Be(25); // 5x5 hall
        result.Data.AvailableSeats.Should().HaveCount(25);
        result.Data.ReservedSeats.Should().BeEmpty();
        result.Data.BookedSeats.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeatAvailability_InvalidShowtime_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidShowtimeId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/bookings/availability/{invalidShowtimeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReservation_ValidRequest_ReturnsReservation()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1", "A2", "A3" }
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SeatNumbers.Should().BeEquivalentTo(new[] { "A1", "A2", "A3" });
        result.Data.Status.Should().Be("Pending");
        result.Data.TotalAmount.Should().Be(30.00m); // 3 seats × $10
        result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateReservation_InvalidSeatNumbers_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "Z99" } // Invalid seat
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateReservation_MoreThan10Seats_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var tooManySeats = Enumerable.Range(1, 11).Select(i => $"A{i}").ToList();
        var reservationDto = new CreateReservationDto(showtimeId, tooManySeats);

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("more than 10 seats"));
    }

    [Fact]
    public async Task CreateReservation_DuplicateSeatNumbers_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1", "A2", "A1" } // Duplicate A1
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Duplicate"));
    }

    [Fact]
    public async Task CreateReservation_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var setupClient = _factory.CreateClient();
        var (showtimeId, _) = await TestDataHelper.SetupBookingScenarioAsync(setupClient, _factory.Services);

        // Create a completely fresh client with no authentication for the actual test
        var unauthenticatedClient = _factory.CreateClient();
        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1" }
        );

        // Act - No authorization header
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReservation_AlreadyReservedSeats_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1", "A2" }
        );

        // First reservation
        await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Act - Try to reserve same seats
        var response = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().Contain("not available");
    }

    [Fact]
    public async Task CancelReservation_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1", "A2" }
        );

        var createResponse = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        var reservationId = createResult!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/bookings/reserve/{reservationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify seats are available again
        var availabilityResponse = await client.GetAsync($"/api/bookings/availability/{showtimeId}");
        var availabilityResult = await availabilityResponse.Content.ReadFromJsonAsync<ApiResponse<SeatAvailabilityDto>>();
        availabilityResult!.Data!.AvailableSeats.Should().Contain(s => s.SeatNumber == "A1");
        availabilityResult.Data.AvailableSeats.Should().Contain(s => s.SeatNumber == "A2");
    }

    [Fact]
    public async Task CancelReservation_NonExistentReservation_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await TestDataHelper.RegisterAndLoginAsync(client, "user@test.com");

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var invalidReservationId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/bookings/reserve/{invalidReservationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelReservation_OtherUsersReservation_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1) = await TestDataHelper.SetupBookingScenarioAsync(client, _factory.Services);

        // User 1 creates reservation
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var reservationDto = new CreateReservationDto(
            showtimeId,
            new List<string> { "A1", "A2" }
        );

        var createResponse = await client.PostAsJsonAsync("/api/bookings/reserve", reservationDto);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        var reservationId = createResult!.Data!.Id;

        // User 2 tries to cancel User 1's reservation
        var token2 = await TestDataHelper.RegisterAndLoginAsync(client, $"user2-{Guid.NewGuid()}@test.com");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await client.DeleteAsync($"/api/bookings/reserve/{reservationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Error.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task CancelReservation_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var reservationId = Guid.NewGuid();

        // Act - No authorization header
        var response = await client.DeleteAsync($"/api/bookings/reserve/{reservationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
