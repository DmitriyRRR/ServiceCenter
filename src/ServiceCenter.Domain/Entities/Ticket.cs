using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;

    public int DeviceId { get; set; }
    public virtual Device Device { get; set; } = null!;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public string? AssignedEngineerId { get; set; }
    public virtual ApplicationUser? AssignedEngineer { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    public string ProblemDescription { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }

    public DateTime TimeIn { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedTimeOut { get; set; }
    public DateTime? ActualTimeOut { get; set; }

    public decimal? EstimatedPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public virtual ICollection<TicketPart> TicketParts { get; set; } = new List<TicketPart>();
}
