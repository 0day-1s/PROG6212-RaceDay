namespace RaceDay.API.Models;

public class AppUser
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"
    public string? PhoneNumber { get; set; }
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

    public ICollection<Event> EventsOrganised { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}