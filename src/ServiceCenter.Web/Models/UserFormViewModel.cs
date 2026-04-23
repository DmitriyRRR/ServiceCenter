using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.Web.Models;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100), Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100), Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Required on Create, optional on Edit (leave blank to keep current)
    [MinLength(8), Display(Name = "Password")]
    public string? Password { get; set; }
}
