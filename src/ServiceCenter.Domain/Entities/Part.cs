using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Domain.Entities;

public class Part
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public int QuantityInStock { get; set; }
    public int LowStockThreshold { get; set; } = 2;
    public decimal UnitPrice { get; set; }
    public PartStatus Status { get; set; } = PartStatus.InStock;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TicketPart> TicketParts { get; set; } = new List<TicketPart>();
    public virtual ICollection<PartOrder> PartOrders { get; set; } = new List<PartOrder>();
}
