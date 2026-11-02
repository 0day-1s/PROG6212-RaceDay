namespace RaceDay.Mvc.ViewModels;

public class OrganiserDashboardVm
{
    public List<OrganiserEventSummaryVm> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public OrganiserEventSummaryVm? NextEvent =>
        Events.Where(e => e.EventDate.Date >= DateTime.Today).OrderBy(e => e.EventDate).FirstOrDefault();
}

public class OrganiserEventSummaryVm
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int EnrolmentCount { get; set; }
}