using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class EventFormVm
{
    [Required, Display(Name = "Event name")]

    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Event date")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Required, Range(0.1, 1000, ErrorMessage = "Enter a distance between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a venue.")]
    [Display(Name = "Venue")]
    public int VenueId { get; set; }

    [ValidateNever]
    public List<VenueDto> Venues { get; set; } = new();
}