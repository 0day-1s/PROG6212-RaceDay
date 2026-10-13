using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.API.Tests;

public static class TestHelpers
{
    public static async Task<HttpClient> RegisterAndLoginAsync(
        HttpClient client, string email, string role)
    {
        var registerRequest = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = "Test1234!",
            Role = role
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }
}