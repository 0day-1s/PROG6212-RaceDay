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
            var enrolResponse = await _api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{e.EventId}");
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

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        var expired = SessionExpired(response); if (expired != null) return expired;

        var e = await _api.ReadAsync<EventDto>(response);
        if (e == null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var vm = new EventFormVm
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            EventType = e.EventType,
            Distance = e.Distance,
            VenueId = e.VenueId
        };

        return View("CreateEvent", await LoadVenuesAsync(vm));
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormVm vm)
    {
        var guard = Guard(); if (guard != null) return guard;

        vm.EventId = id;
        if (!ModelState.IsValid) return View("CreateEvent", await LoadVenuesAsync(vm));

        var request = new UpdateEventRequest
        {
            EventName = vm.EventName,
            Description = vm.Description,
            EventDate = vm.EventDate,
            EventType = vm.EventType,
            Distance = vm.Distance,
            VenueId = vm.VenueId
        };

        var response = await _api.SendAsync(HttpMethod.Put, $"api/events/{id}", request);
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ReadErrorAsync(response));
            return View("CreateEvent", await LoadVenuesAsync(vm));
        }

        TempData["Success"] = "Event updated successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Event deleted.";
        else
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);

        return RedirectToAction(nameof(Events));
    }


    public async Task<IActionResult> Categories(int eventId)
    {
        var guard = Guard(); if (guard != null) return guard;

        var eventResponse = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}");
        var expired = SessionExpired(eventResponse); if (expired != null) return expired;

        var ev = await _api.ReadAsync<EventDto>(eventResponse);
        if (ev == null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/categories");
        var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new();

        return View(new CategoriesPageVm
        {
            EventId = eventId,
            EventName = ev.EventName,
            Categories = categories
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, string categoryName, string? description)
    {
        var guard = Guard(); if (guard != null) return guard;

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories",
            new CreateCategoryRequest { CategoryName = categoryName, Description = description });
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category added.";
        else
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/categories");
        var expired = SessionExpired(response); if (expired != null) return expired;

        var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new();
        var category = categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        return View(new CategoryFormVm
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            CategoryName = category.CategoryName,
            Description = category.Description
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(CategoryFormVm vm)
    {
        var guard = Guard(); if (guard != null) return guard;

        if (!ModelState.IsValid) return View(vm);

        var response = await _api.SendAsync(HttpMethod.Put, $"api/categories/{vm.CategoryId}",
            new CreateCategoryRequest { CategoryName = vm.CategoryName, Description = vm.Description });
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ReadErrorAsync(response));
            return View(vm);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var guard = Guard(); if (guard != null) return guard;

        var response = await _api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");
        var expired = SessionExpired(response); if (expired != null) return expired;

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category deleted.";
        else
            TempData["Error"] = await RaceDayApiClient.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId });
    }
}