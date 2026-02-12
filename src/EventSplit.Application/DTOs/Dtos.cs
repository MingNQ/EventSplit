namespace EventSplit.Application.DTOs;

// --- Auth DTOs ---
public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record AuthResponse(string Token, string RefreshToken, string FullName, string Email, Guid UserId);

// --- Event DTOs ---
public record CreateEventRequest(
    string Title,
    string? Description,
    decimal TotalAmount,
    DateTime Deadline,
    string? BankCode,
    string? BankAccountNumber,
    string? BankAccountName,
    bool CreatorJoins = false,
    List<ExpenseItemRequest>? InitialExpenses = null
);

public record ExpenseItemRequest(string Description, decimal Amount);

public record UpdateEventRequest(
    string Title,
    string? Description,
    decimal TotalAmount,
    DateTime Deadline,
    string? BankCode,
    string? BankAccountNumber,
    string? BankAccountName
);

public record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    decimal TotalAmount,
    DateTime Deadline,
    string? BankCode,
    string? BankAccountNumber,
    string? BankAccountName,
    DateTime CreatedAt,
    Guid CreatorId,
    string CreatorName,
    List<ParticipantResponse> Participants,
    List<ExpenseResponse> Expenses
);

public record EventSummaryResponse(
    Guid Id,
    string Title,
    decimal TotalAmount,
    DateTime Deadline,
    DateTime CreatedAt,
    int ParticipantCount,
    int PaidCount,
    bool IsCreator
);

// --- Participant DTOs ---
public record AddParticipantRequest(string Email);
public record ParticipantResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    decimal AmountToPay,
    string PaymentStatus,
    DateTime? PaidAt
);

// --- Expense DTOs ---
public record CreateExpenseRequest(string Description, decimal Amount);
public record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string CreatedByName,
    DateTime CreatedAt
);

// --- Notification DTOs ---
public record NotificationResponse(
    Guid Id,
    string Message,
    string Type,
    Guid? EventId,
    bool IsRead,
    DateTime CreatedAt
);
