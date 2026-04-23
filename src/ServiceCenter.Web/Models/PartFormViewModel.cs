using System.ComponentModel.DataAnnotations;
using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Web.Models;

public class PartFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SKU { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, Range(0, 999999), Display(Name = "Quantity In Stock")]
    public int QuantityInStock { get; set; }

    [Required, Range(0, 999999), Display(Name = "Low Stock Threshold")]
    public int LowStockThreshold { get; set; } = 2;

    [Required, Range(0, 999999.99), Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public PartStatus Status { get; set; } = PartStatus.InStock;
}
