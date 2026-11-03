using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class EnrolmentsPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultsPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<ResultDto> Results { get; set; } = new();
    public List<EnrolmentDto> Awaiting { get; set; } = new();
}