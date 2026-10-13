using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class EnrolmentsControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentsControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated()
    {
        await TestDataSeeder.SeedVenueAsync(_factory, "Enrolment Test Venue");

        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventRequest = new CreateEventRequest
        {
            EventName = "Enrolment Test Event",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 10,
            VenueId = 1
        };
        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", eventRequest);
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventDto>();

        var categoryRequest = new CreateCategoryRequest { CategoryName = "10km" };
        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories", categoryRequest);
        var createdCategory = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, $"part{Guid.NewGuid()}@test.com", "Participant");

        var enrolRequest = new CreateEnrolmentRequest { CategoryId = createdCategory!.CategoryId };
        var enrolResponse = await participantClient.PostAsJsonAsync(
            $"/api/events/{createdEvent.EventId}/enrolments", enrolRequest);

        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_AsParticipant_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"wrongrole{Guid.NewGuid()}@test.com", "Participant");

        var response = await client.GetAsync("/api/enrolments/event/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}