using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class EnrolmentNamesTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentNamesTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_AsOwner_IncludesNames()
    {
        await TestDataSeeder.SeedVenueAsync(_factory, "Names Test Venue");

        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            EventName = "Names Test Event",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 10,
            VenueId = 1
        });
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventDto>();

        var categoryResponse = await organiser.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories",
            new CreateCategoryRequest { CategoryName = "10km" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");
        var enrolResponse = await participant.PostAsJsonAsync(
            $"/api/events/{createdEvent.EventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = category!.CategoryId });
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        var listResponse = await organiser.GetAsync($"/api/enrolments/event/{createdEvent.EventId}");
        var enrolments = await listResponse.Content.ReadFromJsonAsync<List<EnrolmentDto>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var single = Assert.Single(enrolments!);
        Assert.False(string.IsNullOrWhiteSpace(single.ParticipantName));
        Assert.Equal("10km", single.CategoryName);
        Assert.Equal("Names Test Event", single.EventName);
    }
}