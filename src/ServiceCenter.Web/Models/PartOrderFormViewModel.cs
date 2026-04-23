using System.ComponentModel.DataAnnotations;
using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Web.Models;

public class PartOrderFormViewModel
{
    public int Id { get; set; }

    [Required, Display(Name = "Part")]
    public int PartId { get; set; }

    [Required, Range(1, 999999)]
    public int Quantity { get; set; } = 1;

    public PartOrderStatus Status { get; set; } = PartOrderStatus.Pending;

    [MaxLength(200), Display(Name = "Supplier")]
    public string? SupplierName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Expected Delivery")]
    public DateTime? ExpectedAt { get; set; }

    [Display(Name = "Received At")]
    public DateTime? ReceivedAt { get; set; }
}
