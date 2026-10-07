namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string EnrolmentStatus { get; set; } = "Pending"; // Pending, Confirmed

    public int ParticipantId { get; set; }
    public AppUser Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}