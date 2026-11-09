using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class UploadsControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private static readonly byte[] TinyBytes = { 1, 2, 3, 4 };
    private readonly RaceDayWebApplicationFactory _factory;

    public UploadsControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent FilePayload(string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(TinyBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return content;
    }

    private async Task<int> CreateEventAsync(HttpClient organiserClient)
    {
        await TestDataSeeder.SeedVenueAsync(_factory, "Upload Test Venue");

        var request = new CreateEventRequest
        {
            EventName = "Upload Test Event",
            EventDate = DateTime.UtcNow.AddMonths(1),
            EventType = "Run",
            Distance = 5,
            VenueId = 1
        };

        var response = await organiserClient.PostAsJsonAsync("/api/events", request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        return created!.EventId;
    }

    [Fact]
    public async Task UploadBanner_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/events/1/banner", FilePayload("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"part{Guid.NewGuid()}@test.com", "Participant");

        var response = await client.PostAsync("/api/events/1/banner", FilePayload("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForMissingEvent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var response = await client.PostAsync("/api/events/999999/banner", FilePayload("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsOrganiserWhoDoesNotOwnEvent_ReturnsForbidden()
    {
        var owner = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(owner, $"owner{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(owner);

        var otherOrganiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherOrganiser, $"other{Guid.NewGuid()}@test.com", "Organiser");

        var response = await otherOrganiser.PostAsync(
            $"/api/events/{eventId}/banner", FilePayload("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageFile_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(client);

        var response = await client.PostAsync(
            $"/api/events/{eventId}/banner", FilePayload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/profile/me/picture", FilePayload("me.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WithNonImageFile_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"pic{Guid.NewGuid()}@test.com", "Participant");

        var response = await client.PostAsync("/api/profile/me/picture", FilePayload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetImage_OutsideAllowedFolders_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/images/secrets/appsettings.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}