using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class VenuesControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public VenuesControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetVenues_ReturnsSeededVenue()
    {
        await TestDataSeeder.SeedVenueAsync(_factory, "Venues Test Venue");

        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, $"venue{Guid.NewGuid()}@test.com", "Participant");

        var response = await client.GetAsync("/api/venues");
        var venues = await response.Content.ReadFromJsonAsync<List<VenueDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(venues);
        Assert.NotEmpty(venues!);
    }
}