namespace RaceDay.API.Models;

public class Result
{
    public int ResultId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public int? TotalFinishers { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public int CapturedBy { get; set; }
    public AppUser CapturedByUser { get; set; } = null!;
}