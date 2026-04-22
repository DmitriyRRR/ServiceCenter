using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;

namespace ServiceCenter.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<TicketPart> TicketParts => Set<TicketPart>();
    public DbSet<PartOrder> PartOrders => Set<PartOrder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ticket>(e =>
        {
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.Property(t => t.TotalPrice).HasPrecision(18, 2);
            e.Property(t => t.EstimatedPrice).HasPrecision(18, 2);
            e.HasOne(t => t.AssignedEngineer)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedEngineerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            // Restrict to avoid multiple cascade paths (Client→Device→Ticket)
            e.HasOne(t => t.Client)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Device)
                .WithMany(d => d.Tickets)
                .HasForeignKey(t => t.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkItem>(e =>
        {
            e.Property(w => w.Price).HasPrecision(18, 2);
            e.HasOne(w => w.Engineer)
                .WithMany(u => u.WorkItems)
                .HasForeignKey(w => w.EngineerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(w => w.Ticket)
                .WithMany(t => t.WorkItems)
                .HasForeignKey(w => w.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TicketPart>(e =>
        {
            e.Property(tp => tp.UnitPriceAtTime).HasPrecision(18, 2);
            e.HasOne(tp => tp.Ticket)
                .WithMany(t => t.TicketParts)
                .HasForeignKey(tp => tp.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Part>(e =>
        {
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });

        builder.Entity<WorkType>(e =>
        {
            e.Property(w => w.DefaultPrice).HasPrecision(18, 2);
        });

        builder.Entity<Client>(e =>
        {
            e.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
