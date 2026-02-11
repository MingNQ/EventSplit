using EventSplit.Domain.Entities;

namespace EventSplit.Application.Interfaces;

public interface IAppDbContext
{
    Microsoft.EntityFrameworkCore.DbSet<User> Users { get; }
    Microsoft.EntityFrameworkCore.DbSet<Event> Events { get; }
    Microsoft.EntityFrameworkCore.DbSet<EventParticipant> EventParticipants { get; }
    Microsoft.EntityFrameworkCore.DbSet<EventExpense> EventExpenses { get; }
    Microsoft.EntityFrameworkCore.DbSet<RefreshToken> RefreshTokens { get; }
    Microsoft.EntityFrameworkCore.DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
