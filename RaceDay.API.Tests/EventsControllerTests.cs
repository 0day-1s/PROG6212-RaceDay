using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class EventsControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventsControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEvents_WithoutAuthentication_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateEventRequest
        {
            EventName = "Unauthorised Event",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 10,
            VenueId = 1
        };

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"participant{Guid.NewGuid()}@test.com", "Participant");

        var request = new CreateEventRequest
        {
            EventName = "Should Not Be Created",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 10,
            VenueId = 1
        };

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"organiser{Guid.NewGuid()}@test.com", "Organiser");

        await TestDataSeeder.SeedVenueAsync(_factory, "Test Venue");

        var request = new CreateEventRequest
        {
            EventName = "Organiser's Event",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 10,
            VenueId = 1
        };

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}