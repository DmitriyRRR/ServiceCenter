using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Domain.Entities;

public class PartOrder
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public virtual Part Part { get; set; } = null!;

    public int Quantity { get; set; }
    public PartOrderStatus Status { get; set; } = PartOrderStatus.Ordered;
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public string? CreatedById { get; set; }
    public virtual ApplicationUser? CreatedBy { get; set; }
}
