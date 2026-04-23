using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.Web.Models;

public class DeviceFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Client")]
    public int ClientId { get; set; }

    [Required, MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
