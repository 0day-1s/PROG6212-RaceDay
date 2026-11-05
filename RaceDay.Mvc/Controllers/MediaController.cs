using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class MediaController : Controller
{
    private readonly RaceDayApiClient _api;

    public MediaController(RaceDayApiClient api) => _api = api;

    private IActionResult? Guard(string? requiredRole)
    {
        var role = HttpContext.Session.GetString("Role");
        if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Auth");
        if (requiredRole != null && role != requiredRole) return RedirectToAction("AccessDenied", "Home");
        return null;
    }

    private IActionResult? SessionExpired(HttpResponseMessage response)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return null;
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }

    //  Event banner (Organiser) 
    [HttpGet]
    public async Task<IActionResult> Banner(int eventId)
    {
        var guard = Guard("Organiser"); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}");
        var expired = SessionExpired(response); if (expired != null) return expired;

        var ev = await _api.ReadAsync<EventDto>(response);
        if (ev == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Events", "Organiser");
        }

        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> Banner(int eventId, IFormFile banner)
    {
        var guard = Guard("Organiser"); if (guard != null) return guard;

        if (banner == null || banner.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Banner), new { eventId });
        }

        var response = await _api.UploadFileAsync($"api/events/{eventId}/banner", banner);
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);
        else
            TempData["Success"] = "Banner uploaded.";

        return RedirectToAction(nameof(Banner), new { eventId });
    }

    // Profile picture (any logged-in user) 
    [HttpPost]
    public async Task<IActionResult> UploadProfilePicture(IFormFile picture)
    {
        var guard = Guard(null); if (guard != null) return guard;

        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction("Profile", "Participant");
        }

        var response = await _api.UploadFileAsync("api/profile/me/picture", picture);
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);
        else
            TempData["Success"] = "Profile picture updated.";

        return RedirectToAction("Profile", "Participant");
    }
}