namespace ServiceCenter.Domain.Enums;

public enum TicketStatus
{
    New = 1,
    InProgress = 2,
    WaitingForParts = 3,
    Ready = 4,
    Closed = 5,
    Cancelled = 6
}
