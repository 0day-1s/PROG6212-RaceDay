using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests;

public static class TestDataSeeder
{
    public static async Task SeedVenueAsync(RaceDayWebApplicationFactory factory, string venueName)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        if (!await context.Venues.AnyAsync())
        {
            context.Venues.Add(new Venue
            {
                VenueName = venueName,
                AddressLine = "123 Test Street",
                City = "Cape Town",
                Province = "Western Cape"
            });
            await context.SaveChangesAsync();
        }
    }
}