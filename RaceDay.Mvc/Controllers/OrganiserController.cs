using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;
    public OrganiserController(RaceDayApiClient api) => _api = api;

    private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId").HasValue;

    private bool IsOrganiser() =>
        string.Equals(HttpContext.Session.GetString("Role"), "Organiser", StringComparison.OrdinalIgnoreCase);

    // Returns a redirect when the user may not see Organiser pages, otherwise null.
    private IActionResult? Guard()
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
        if (!IsOrganiser()) return RedirectToAction("AccessDenied", "Home");
        return null;
    }

    // If the API says 401 the API session ended: clear ours and ask the user to log in again.
    private IActionResult? SessionExpired(HttpResponseMessage response)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return null;
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }

    public async Task<IActionResult> Index()
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(response); if (expired != null) return expired;
        var events = await _api.ReadAsync<List<EventDto>>(response) ?? new();

        var vm = new OrganiserDashboardVm();
        foreach (var e in events.OrderBy(e => e.EventDate))
        {
            var enrolResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{e.EventId}/enrolments");
            var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(enrolResponse) ?? new();

            vm.Events.Add(new OrganiserEventSummaryVm
            {
                EventId = e.EventId,
                Name = e.EventName,
                EventDate = e.EventDate,
                EnrolmentCount = enrolments.Count
            });
        }

        return View(vm);
    }

    public async Task<IActionResult> Events()
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
        var expired = SessionExpired(response); if (expired != null) return expired;
        var events = await _api.ReadAsync<List<EventDto>>(response) ?? new();

        var venueResponse = await _api.SendAsync(HttpMethod.Get, "api/venues");
        var venues = await _api.ReadAsync<List<VenueDto>>(venueResponse) ?? new();
        ViewBag.VenueNames = venues.ToDictionary(v => v.VenueId, v => v.VenueName);

        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    private async Task<EventFormVm> LoadVenuesAsync(EventFormVm vm)
    {
        var response = await _api.SendAsync(HttpMethod.Get, "api/venues");
        vm.Venues = await _api.ReadAsync<List<VenueDto>>(response) ?? new();
        return vm;
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        var guard = Guard(); if (guard != null) return guard;
        return View(await LoadVenuesAsync(new EventFormVm()));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        var guard = Guard(); if (guard != null) return guard;

        if (!ModelState.IsValid) return View(await LoadVenuesAsync(vm));

        var request = new CreateEventRequest
        {
            EventName = vm.EventName,
            Description = vm.Description,
            EventDate = vm.EventDate,
            EventType = vm.EventType,
            Distance = vm.Distance,
            VenueId = vm.VenueId
        };

        var response = await _api.SendAsync(HttpMethod.Post, "api/events", request);
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ReadErrorAsync(response));
            return View(await LoadVenuesAsync(vm));
        }

        TempData["Success"] = "Event created successfully.";
        return RedirectToAction(nameof(Events));
    }
}