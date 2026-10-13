using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.API.Tests;

public class AuthControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Mokoena",
            Email = $"thabo{Guid.NewGuid()}@test.com",
            Password = "Test1234!",
            Role = "Participant"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup{Guid.NewGuid()}@test.com";
        var request = new RegisterRequest
        {
            FirstName = "Lindiwe",
            LastName = "Dube",
            Email = email,
            Password = "Test1234!",
            Role = "Participant"
        };

        await client.PostAsJsonAsync("/api/auth/register", request);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"valid{Guid.NewGuid()}@test.com";

        await TestHelpers.RegisterAndLoginAsync(client, email, "Participant");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}