using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;
    public ParticipantController(RaceDayApiClient api) => _api = api;

    private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId").HasValue;

    private bool IsParticipant() =>
        string.Equals(HttpContext.Session.GetString("Role"), "Participant", StringComparison.OrdinalIgnoreCase);

    private IActionResult? Guard()
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
        if (!IsParticipant()) return RedirectToAction("AccessDenied", "Home");
        return null;
    }

    private IActionResult? SessionExpired(HttpResponseMessage response)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return null;
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }

    private async Task<Dictionary<int, string>> LoadVenueNamesAsync()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/venues");
        var venues = await _api.ReadAsync<List<VenueDto>>(response) ?? new();
        return venues.ToDictionary(v => v.VenueId, v => v.VenueName);
    }

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/events");
        var events = await _api.ReadAsync<List<EventDto>>(response) ?? new();
        var venueNames = await LoadVenueNamesAsync();

        events = events.Where(e => e.EventDate.Date >= DateTime.Today).ToList();

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                venueNames.GetValueOrDefault(e.VenueId, "").Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new ParticipantEventsVm
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            VenueNames = venueNames,
            Type = type,
            Search = search
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var guard = Guard(); if (guard != null) return guard;

        var eventResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        var ev = await _api.ReadAsync<EventDto>(eventResponse);
        if (ev == null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var categoryResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}/categories");
        var categories = await _api.ReadAsync<List<CategoryDto>>(categoryResponse) ?? new();

        var enrolResponse = await _api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        var expired = SessionExpired(enrolResponse); if (expired != null) return expired;
        var mine = await _api.ReadAsync<List<EnrolmentDto>>(enrolResponse) ?? new();

        var venueNames = await LoadVenueNamesAsync();

        return View(new EventDetailsVm
        {
            Event = ev,
            VenueName = venueNames.GetValueOrDefault(ev.VenueId, ""),
            Categories = categories,
            MyEnrolment = mine.FirstOrDefault(en => en.EventId == id)
        });
    }

    [HttpPost]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = categoryId });
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You have entered the event. The organiser will confirm your entry.";
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyEvents()
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        var expired = SessionExpired(response); if (expired != null) return expired;
        var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? new();

        return View(enrolments.OrderBy(en => en.EventDate).ToList());
    }


    public async Task<IActionResult> MyResults()
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/results/mine");
        var expired = SessionExpired(response); if (expired != null) return expired;
        var results = await _api.ReadAsync<List<ResultDto>>(response) ?? new();

        return View(results.OrderByDescending(r => r.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/profile/me");
        var expired = SessionExpired(response); if (expired != null) return expired;

        var p = await _api.ReadAsync<ProfileDto>(response);
        if (p == null)
        {
            TempData["Error"] = "Your profile could not be loaded.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ProfileVm
        {
            Email = p.Email,
            Role = p.Role,
            FirstName = p.FirstName,
            LastName = p.LastName,
            PhoneNumber = p.PhoneNumber,
            ProfileImageUrl = p.ProfileImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        var guard = Guard(); if (guard != null) return guard;

        if (!ModelState.IsValid) return View(vm);

        var response = await _api.SendAsync(HttpMethod.Put, "api/profile/me", new UpdateProfileRequest
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            PhoneNumber = vm.PhoneNumber
        });
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ReadErrorAsync(response));
            return View(vm);
        }

        HttpContext.Session.SetString("DisplayName", vm.FirstName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}