namespace EventSplit.Domain.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Deadline { get; set; }
    public string? BankCode { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    public ICollection<EventExpense> Expenses { get; set; } = new List<EventExpense>();
}
