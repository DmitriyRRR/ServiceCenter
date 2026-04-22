namespace ServiceCenter.Domain.Entities;

public class WorkItem
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public int WorkTypeId { get; set; }
    public virtual WorkType WorkType { get; set; } = null!;

    public string? Description { get; set; }
    public decimal Price { get; set; }

    public string? EngineerId { get; set; }
    public virtual ApplicationUser? Engineer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
