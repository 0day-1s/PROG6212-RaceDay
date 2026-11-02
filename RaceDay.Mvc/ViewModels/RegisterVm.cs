using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class RegisterVm
{
    [Required, Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Participant";

    [Phone, Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}