namespace ServiceCenter.Domain.Entities;

public class TicketPart
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public int PartId { get; set; }
    public virtual Part Part { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPriceAtTime { get; set; }
    public decimal TotalPrice => Quantity * UnitPriceAtTime;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
