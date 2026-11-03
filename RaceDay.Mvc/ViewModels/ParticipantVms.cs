using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class ParticipantEventsVm
{
    public List<EventDto> Events { get; set; } = new();
    public Dictionary<int, string> VenueNames { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public string VenueName { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}