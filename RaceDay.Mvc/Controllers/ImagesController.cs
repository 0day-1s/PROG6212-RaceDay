using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class ImagesController : Controller
{
    private const string Marker = "/raceday-images/";
    private readonly RaceDayApiClient _api;

    public ImagesController(RaceDayApiClient api) => _api = api;

    // GET /Images/Show?url=<blob url stored in the database>
    public async Task<IActionResult> Show(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return NotFound();

        var i = url.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return NotFound();

        var blobName = url[(i + Marker.Length)..];
        if (blobName.Contains("..")) return NotFound();

        var response = await _api.SendAsync(HttpMethod.Get, $"api/images/{blobName}");
        if (!response.IsSuccessStatusCode) return NotFound();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var type = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";

        Response.Headers.CacheControl = "public,max-age=3600";
        return File(bytes, type);
    }
}