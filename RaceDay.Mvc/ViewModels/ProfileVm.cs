using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class ProfileVm
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [Required, Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Phone, Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}