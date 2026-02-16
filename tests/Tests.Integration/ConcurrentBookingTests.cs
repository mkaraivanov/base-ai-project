using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Reservations;
using Application.DTOs.Seats;
using Backend.Models;
using FluentAssertions;
using Tests.Integration.Helpers;
using Xunit;

namespace Tests.Integration;

public class ConcurrentBookingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConcurrentBookingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReservation_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1, token2) = await TestDataHelper.SetupConcurrentBookingScenarioAsync(client, _factory.Services);

        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });

        // Create two separate clients for concurrent requests
        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        // Act - Simulate concurrent requests using Task.WhenAll
        var task1 = client1.PostAsJsonAsync("/api/bookings/reserve", dto);
        var task2 = client2.PostAsJsonAsync("/api/bookings/reserve", dto);

        var responses = await Task.WhenAll(task1, task2);

        // Assert
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failureCount = responses.Count(r => !r.IsSuccessStatusCode);

        successCount.Should().Be(1, "only one user should successfully reserve the seats");
        failureCount.Should().Be(1, "the other user should fail due to concurrency conflict");

        // Verify the failed request has appropriate error message
        var failedResponse = responses.First(r => !r.IsSuccessStatusCode);
        failedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var failedResult = await failedResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        failedResult.Should().NotBeNull();
        failedResult!.Success.Should().BeFalse();
        failedResult.Error.Should().ContainAny("seats are no longer available", "not available");

        // Verify the successful reservation
        var successResponse = responses.First(r => r.IsSuccessStatusCode);
        var successResult = await successResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        successResult.Should().NotBeNull();
        successResult!.Success.Should().BeTrue();
        successResult.Data.Should().NotBeNull();
        successResult.Data!.SeatNumbers.Should().BeEquivalentTo(new[] { "A1", "A2" });
    }

    [Fact]
    public async Task CreateReservation_ConcurrentRequestsDifferentSeats_BothSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1, token2) = await TestDataHelper.SetupConcurrentBookingScenarioAsync(client, _factory.Services);

        var dto1 = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });
        var dto2 = new CreateReservationDto(showtimeId, new List<string> { "B1", "B2" });

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        // Act - Concurrent requests for different seats
        var task1 = client1.PostAsJsonAsync("/api/bookings/reserve", dto1);
        var task2 = client2.PostAsJsonAsync("/api/bookings/reserve", dto2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - Both should succeed since they're booking different seats
        responses.Should().AllSatisfy(r => r.IsSuccessStatusCode.Should().BeTrue());

        var result1 = await responses[0].Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        var result2 = await responses[1].Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();

        result1!.Data!.SeatNumbers.Should().BeEquivalentTo(new[] { "A1", "A2" });
        result2!.Data!.SeatNumbers.Should().BeEquivalentTo(new[] { "B1", "B2" });

        // Verify seat availability
        var availabilityResponse = await client.GetAsync($"/api/bookings/availability/{showtimeId}");
        var availabilityResult = await availabilityResponse.Content.ReadFromJsonAsync<ApiResponse<SeatAvailabilityDto>>();

        availabilityResult!.Data!.ReservedSeats.Should().HaveCount(4);
        availabilityResult.Data.ReservedSeats.Should().Contain(s => s.SeatNumber == "A1");
        availabilityResult.Data.ReservedSeats.Should().Contain(s => s.SeatNumber == "A2");
        availabilityResult.Data.ReservedSeats.Should().Contain(s => s.SeatNumber == "B1");
        availabilityResult.Data.ReservedSeats.Should().Contain(s => s.SeatNumber == "B2");
    }

    [Fact]
    public async Task CreateReservation_ConcurrentRequestsOverlappingSeats_OnlyOneSucceeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1, token2) = await TestDataHelper.SetupConcurrentBookingScenarioAsync(client, _factory.Services);

        // Both users want A2, but different additional seats
        var dto1 = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });
        var dto2 = new CreateReservationDto(showtimeId, new List<string> { "A2", "A3" });

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        // Act - Concurrent requests with overlapping seat (A2)
        var task1 = client1.PostAsJsonAsync("/api/bookings/reserve", dto1);
        var task2 = client2.PostAsJsonAsync("/api/bookings/reserve", dto2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - Only one should succeed
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failureCount = responses.Count(r => !r.IsSuccessStatusCode);

        successCount.Should().Be(1, "only one user should successfully reserve when seats overlap");
        failureCount.Should().Be(1, "the other user should fail");
    }

    [Fact]
    public async Task CreateReservation_AfterCancellation_SeatsAvailableAgain()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (showtimeId, token1, token2) = await TestDataHelper.SetupConcurrentBookingScenarioAsync(client, _factory.Services);

        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        // User 1 reserves seats
        var createResponse = await client1.PostAsJsonAsync("/api/bookings/reserve", dto);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        var reservationId = createResult!.Data!.Id;

        // User 1 cancels reservation
        var cancelResponse = await client1.DeleteAsync($"/api/bookings/reserve/{reservationId}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - User 2 tries to reserve the same seats
        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        var user2Response = await client2.PostAsJsonAsync("/api/bookings/reserve", dto);

        // Assert - User 2 should succeed
        user2Response.IsSuccessStatusCode.Should().BeTrue();

        var user2Result = await user2Response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
        user2Result!.Data!.SeatNumbers.Should().BeEquivalentTo(new[] { "A1", "A2" });
    }

    [Fact]
    public async Task CreateReservation_HighConcurrency_MultipleUsers_CorrectBehavior()
    {
        // Arrange
        var client = _factory.CreateClient();
        var adminToken = await TestDataHelper.RegisterAndLoginAdminAsync(_factory.Services);

        var movieId = await TestDataHelper.CreateMovieAsync(client, adminToken);
        var hallId = await TestDataHelper.CreateHallAsync(client, adminToken);
        var showtimeId = await TestDataHelper.CreateShowtimeAsync(client, adminToken, movieId, hallId);

        // Create 10 users trying to book the same 2 seats
        var userTasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var token = await TestDataHelper.RegisterAndLoginAsync(client, $"user{i}-{Guid.NewGuid()}@test.com");
                return token;
            });

        var userTokens = await Task.WhenAll(userTasks);

        var dto = new CreateReservationDto(showtimeId, new List<string> { "C3", "C4" });

        // Act - 10 concurrent booking attempts for the same seats
        var bookingTasks = userTokens.Select(async token =>
        {
            var userClient = _factory.CreateClient();
            userClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await userClient.PostAsJsonAsync("/api/bookings/reserve", dto);
        });

        var responses = await Task.WhenAll(bookingTasks);

        // Assert - Exactly one should succeed
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failureCount = responses.Count(r => !r.IsSuccessStatusCode);

        successCount.Should().Be(1, "exactly one user should get the seats in high concurrency scenario");
        failureCount.Should().Be(9, "all other users should fail");

        // Verify seat status
        var availabilityResponse = await client.GetAsync($"/api/bookings/availability/{showtimeId}");
        var availabilityResult = await availabilityResponse.Content.ReadFromJsonAsync<ApiResponse<SeatAvailabilityDto>>();

        availabilityResult!.Data!.ReservedSeats.Should().Contain(s => s.SeatNumber == "C3");
        availabilityResult.Data.ReservedSeats.Should().Contain(s => s.SeatNumber == "C4");
        availabilityResult.Data.AvailableSeats.Should().NotContain(s => s.SeatNumber == "C3");
        availabilityResult.Data.AvailableSeats.Should().NotContain(s => s.SeatNumber == "C4");
    }
}
