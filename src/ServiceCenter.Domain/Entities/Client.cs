namespace ServiceCenter.Domain.Entities;

public class Client
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
