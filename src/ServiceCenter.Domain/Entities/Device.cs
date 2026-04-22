namespace ServiceCenter.Domain.Entities;

public class Device
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
