using EventSplit.Application.DTOs;
using EventSplit.Application.Interfaces;
using EventSplit.Domain.Entities;
using EventSplit.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventSplit.Application.Services;

public class EventService
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _emailService;

    public EventService(IAppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid creatorId)
    {
        // Input validation
        if (request.TotalAmount <= 0)
            throw new InvalidOperationException("Tổng số tiền phải lớn hơn 0.");
        if (request.Deadline <= DateTime.UtcNow)
            throw new InvalidOperationException("Hạn thanh toán phải ở tương lai.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Tên sự kiện không được để trống.");

        var creator = await _db.Users.FindAsync(creatorId)
            ?? throw new InvalidOperationException("User not found.");

        var ev = new Event
        {
            Title = request.Title,
            Description = request.Description,
            TotalAmount = request.TotalAmount,
            Deadline = request.Deadline,
            BankCode = request.BankCode,
            BankAccountNumber = request.BankAccountNumber,
            BankAccountName = request.BankAccountName,
            CreatorId = creatorId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return MapToResponse(ev, creator, new List<EventParticipant>(), new List<EventExpense>());
    }

    public async Task<List<EventSummaryResponse>> GetMyEventsAsync(Guid userId)
    {
        var created = await _db.Events
            .Where(e => e.CreatorId == userId)
            .Include(e => e.Participants)
            .ToListAsync();

        var participating = await _db.EventParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.EventId)
            .ToListAsync();

        var participatingEvents = await _db.Events
            .Where(e => participating.Contains(e.Id) && e.CreatorId != userId)
            .Include(e => e.Participants)
            .ToListAsync();

        var all = created.Union(participatingEvents).ToList();

        return all.Select(e => new EventSummaryResponse(
            e.Id, e.Title, e.TotalAmount, e.Deadline, e.CreatedAt,
            e.Participants.Count,
            e.Participants.Count(p => p.PaymentStatus == PaymentStatus.Paid),
            e.CreatorId == userId
        )).OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<EventResponse> GetEventDetailAsync(Guid eventId, Guid userId)
    {
        var ev = await _db.Events
            .Include(e => e.Creator)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .Include(e => e.Expenses).ThenInclude(ex => ex.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");

        var isParticipant = ev.CreatorId == userId || ev.Participants.Any(p => p.UserId == userId);
        if (!isParticipant)
            throw new UnauthorizedAccessException("You don't have access to this event.");

        return MapToResponse(ev, ev.Creator, ev.Participants.ToList(), ev.Expenses.ToList());
    }

    public async Task<EventResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid userId)
    {
        var ev = await _db.Events
            .Include(e => e.Creator)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .Include(e => e.Expenses).ThenInclude(ex => ex.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the creator can update this event.");

        // Input validation
        if (request.TotalAmount <= 0)
            throw new InvalidOperationException("Tổng số tiền phải lớn hơn 0.");
        if (request.Deadline <= DateTime.UtcNow)
            throw new InvalidOperationException("Hạn thanh toán phải ở tương lai.");

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.TotalAmount = request.TotalAmount;
        ev.Deadline = request.Deadline;
        ev.BankCode = request.BankCode;
        ev.BankAccountNumber = request.BankAccountNumber;
        ev.BankAccountName = request.BankAccountName;

        // Recalculate split
        if (ev.Participants.Count > 0)
        {
            var perPerson = Math.Round(ev.TotalAmount / ev.Participants.Count, 0);
            foreach (var p in ev.Participants)
                p.AmountToPay = perPerson;
        }

        await _db.SaveChangesAsync();
        return MapToResponse(ev, ev.Creator, ev.Participants.ToList(), ev.Expenses.ToList());
    }

    public async Task DeleteEventAsync(Guid eventId, Guid userId)
    {
        var ev = await _db.Events
            .Include(e => e.Participants)
            .Include(e => e.Expenses)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the creator can delete this event.");

        _db.EventExpenses.RemoveRange(ev.Expenses);
        _db.EventParticipants.RemoveRange(ev.Participants);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
    }

    public async Task<ParticipantResponse> AddParticipantAsync(Guid eventId, AddParticipantRequest request, Guid userId)
    {
        var ev = await _db.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the creator can add participants.");

        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new InvalidOperationException("User with this email not found. They must register first.");

        if (ev.Participants.Any(p => p.UserId == targetUser.Id))
            throw new InvalidOperationException("User is already a participant.");

        var participant = new EventParticipant
        {
            EventId = eventId,
            UserId = targetUser.Id
        };

        ev.Participants.Add(participant);

        // Recalculate split for all participants
        var count = ev.Participants.Count;
        var perPerson = Math.Round(ev.TotalAmount / count, 0);
        foreach (var p in ev.Participants)
            p.AmountToPay = perPerson;

        await _db.SaveChangesAsync();

        return new ParticipantResponse(
            participant.Id, targetUser.Id, targetUser.FullName, targetUser.Email,
            perPerson, participant.PaymentStatus.ToString(), participant.PaidAt
        );
    }

    public async Task RemoveParticipantAsync(Guid eventId, Guid participantId, Guid userId)
    {
        var ev = await _db.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the creator can remove participants.");

        var participant = ev.Participants.FirstOrDefault(p => p.Id == participantId)
            ?? throw new InvalidOperationException("Participant not found.");

        ev.Participants.Remove(participant);
        _db.EventParticipants.Remove(participant);

        // Recalculate split
        if (ev.Participants.Count > 0)
        {
            var perPerson = Math.Round(ev.TotalAmount / ev.Participants.Count, 0);
            foreach (var p in ev.Participants)
                p.AmountToPay = perPerson;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<ParticipantResponse> ConfirmPaymentAsync(Guid eventId, Guid participantId, Guid userId)
    {
        var participant = await _db.EventParticipants
            .Include(p => p.User)
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.EventId == eventId)
            ?? throw new InvalidOperationException("Participant not found.");

        // Only the event creator or the participant themselves can confirm
        if (participant.Event.CreatorId != userId && participant.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to confirm this payment.");

        if (participant.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Payment already confirmed.");

        participant.PaymentStatus = PaymentStatus.Paid;
        participant.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ParticipantResponse(
            participant.Id, participant.UserId, participant.User.FullName, participant.User.Email,
            participant.AmountToPay, participant.PaymentStatus.ToString(), participant.PaidAt
        );
    }

    // --- Expenses ---
    public async Task<ExpenseResponse> AddExpenseAsync(Guid eventId, CreateExpenseRequest request, Guid userId)
    {
        var ev = await _db.Events.FindAsync(eventId)
            ?? throw new InvalidOperationException("Event not found.");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var expense = new EventExpense
        {
            EventId = eventId,
            Description = request.Description,
            Amount = request.Amount,
            CreatedByUserId = userId
        };

        _db.EventExpenses.Add(expense);
        await _db.SaveChangesAsync();

        return new ExpenseResponse(expense.Id, expense.Description, expense.Amount, user.FullName, expense.CreatedAt);
    }

    // --- Overdue Job ---
    public async Task<List<Guid>> MarkOverduePaymentsAsync()
    {
        var affectedUserIds = new List<Guid>();

        var overdueParticipants = await _db.EventParticipants
            .Include(p => p.Event)
            .Include(p => p.User)
            .Where(p => p.PaymentStatus == PaymentStatus.Pending && p.Event.Deadline < DateTime.UtcNow)
            .ToListAsync();

        foreach (var p in overdueParticipants)
        {
            p.PaymentStatus = PaymentStatus.Overdue;

            // Create in-app notification for overdue payment
            _db.Notifications.Add(new Notification
            {
                UserId = p.UserId,
                EventId = p.EventId,
                Type = "payment_reminder",
                Message = $"Thanh toán cho sự kiện \"{p.Event.Title}\" đã quá hạn! Số tiền: {p.AmountToPay:N0}đ"
            });

            // Also notify the event creator
            _db.Notifications.Add(new Notification
            {
                UserId = p.Event.CreatorId,
                EventId = p.EventId,
                Type = "warning",
                Message = $"{p.User.FullName} chưa thanh toán cho \"{p.Event.Title}\" (quá hạn). Số tiền: {p.AmountToPay:N0}đ"
            });

            // Send email reminder to participant
            if (!string.IsNullOrEmpty(p.User.Email))
            {
                await _emailService.SendEmailAsync(
                    p.User.Email,
                    $"[EventSplit] Nhắc nhở thanh toán - {p.Event.Title}",
                    $@"<div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;padding:24px;background:#1a1a2e;color:#e0e0e0;border-radius:12px;'>
                        <h2 style='color:#ff6b6b;'>⏰ Thanh toán quá hạn!</h2>
                        <p>Xin chào <b>{p.User.FullName}</b>,</p>
                        <p>Khoản thanh toán cho sự kiện <b style='color:#6c63ff;'>{p.Event.Title}</b> đã quá hạn.</p>
                        <div style='background:#16213e;padding:16px;border-radius:8px;margin:16px 0;'>
                            <p>💰 Số tiền: <b style='color:#00d2ff;'>{p.AmountToPay:N0}đ</b></p>
                            <p>📅 Hạn cuối: <b>{p.Event.Deadline:dd/MM/yyyy HH:mm}</b></p>
                        </div>
                        <p>Vui lòng thanh toán sớm nhất có thể.</p>
                        <p style='color:#888;font-size:12px;margin-top:24px;'>— EventSplit</p>
                    </div>"
                );
            }

            affectedUserIds.Add(p.UserId);
            if (!affectedUserIds.Contains(p.Event.CreatorId))
                affectedUserIds.Add(p.Event.CreatorId);
        }

        if (overdueParticipants.Count > 0)
            await _db.SaveChangesAsync();

        return affectedUserIds;
    }

    // --- Helpers ---
    private static EventResponse MapToResponse(Event ev, User creator, List<EventParticipant> participants, List<EventExpense> expenses)
    {
        return new EventResponse(
            ev.Id, ev.Title, ev.Description, ev.TotalAmount, ev.Deadline,
            ev.BankCode, ev.BankAccountNumber, ev.BankAccountName,
            ev.CreatedAt, ev.CreatorId, creator.FullName,
            participants.Select(p => new ParticipantResponse(
                p.Id, p.UserId, p.User?.FullName ?? "", p.User?.Email ?? "",
                p.AmountToPay, p.PaymentStatus.ToString(), p.PaidAt
            )).ToList(),
            expenses.Select(e => new ExpenseResponse(
                e.Id, e.Description, e.Amount, e.CreatedBy?.FullName ?? "", e.CreatedAt
            )).ToList()
        );
    }
}
