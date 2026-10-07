namespace RaceDay.API.Models;

public class Event
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle
    public decimal Distance { get; set; }
    public string? BannerImageUrl { get; set; }

    public int OrganiserId { get; set; }
    public AppUser Organiser { get; set; } = null!;

    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}