using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.Web.Models;

public class WorkTypeFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, Range(0, 999999.99), Display(Name = "Default Price")]
    public decimal DefaultPrice { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
