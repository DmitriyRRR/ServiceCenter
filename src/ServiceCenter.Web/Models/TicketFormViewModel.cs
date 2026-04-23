using System.ComponentModel.DataAnnotations;
using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Web.Models;

public class TicketFormViewModel
{
    public int Id { get; set; }

    [Required, Display(Name = "Client")]
    public int ClientId { get; set; }

    [Required, Display(Name = "Device")]
    public int DeviceId { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Display(Name = "Assigned Engineer")]
    public string? AssignedEngineerId { get; set; }

    [Required, MaxLength(2000), Display(Name = "Problem Description")]
    public string ProblemDescription { get; set; } = string.Empty;

    [MaxLength(2000), Display(Name = "Internal Notes")]
    public string? InternalNotes { get; set; }

    [Display(Name = "Estimated Completion")]
    public DateTime? EstimatedTimeOut { get; set; }

    [Display(Name = "Actual Completion")]
    public DateTime? ActualTimeOut { get; set; }

    [Range(0, 999999), Display(Name = "Estimated Price")]
    public decimal? EstimatedPrice { get; set; }
}
