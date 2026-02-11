using EventSplit.Domain.Enums;

namespace EventSplit.Domain.Entities;

public class EventParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal AmountToPay { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
}
