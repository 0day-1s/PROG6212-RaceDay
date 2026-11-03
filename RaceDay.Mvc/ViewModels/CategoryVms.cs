using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class CategoriesPageVm
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CategoryFormVm
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }
}