using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public class RaceDayApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RaceDayApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _httpContextAccessor = accessor;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        // Forward this user's API session cookie (saved in the MVC session at login).
        string? apiCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(apiCookie))
            request.Headers.TryAddWithoutValidation("Cookie", apiCookie);

        return request;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = CreateRequest(method, url);

        if (body != null)
            request.Content = JsonContent.Create(body);

        return await client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UploadFileAsync(string url, IFormFile file)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = CreateRequest(HttpMethod.Post, url);
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        form.Add(fileContent, "file", file.FileName);
        request.Content = form;
        return await client.SendAsync(request);
    }

    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        string json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    // Turns a failed API response into a short, user-friendly message (no stack traces).
    public static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string body = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');

        if (!string.IsNullOrWhiteSpace(body) && !body.StartsWith("{") && !body.StartsWith("<") && body.Length < 200)
            return body;

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Please log in to continue.",
            System.Net.HttpStatusCode.Forbidden => "You do not have permission to do that.",
            System.Net.HttpStatusCode.NotFound => "That item could not be found.",
            System.Net.HttpStatusCode.Conflict => "That already exists.",
            System.Net.HttpStatusCode.BadRequest => "Some of the details entered are not valid.",
            _ => "Something went wrong. Please try again."
        };
    }

    public static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return null;

        string? sessionHeader = values.FirstOrDefault(v =>
            v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

        // A request Cookie header needs only name=value.
        return sessionHeader?.Split(';', 2)[0];
    }
}