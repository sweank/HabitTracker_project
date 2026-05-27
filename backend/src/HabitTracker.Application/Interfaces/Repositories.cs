using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramChatIdAsync(string telegramChatId, CancellationToken cancellationToken = default);
}

public interface IHabitRepository
{
    Task AddAsync(Habit habit, CancellationToken cancellationToken = default);
    Task<Habit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Habit>> GetUserHabitsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Habit>> GetActiveHabitsWithUsersAsync(CancellationToken cancellationToken = default);
}

public interface IHabitCompletionRepository
{
    Task AddAsync(HabitCompletion completion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HabitCompletion>> GetByHabitIdAsync(Guid habitId, CancellationToken cancellationToken = default);
}

public interface INotificationJobRepository
{
    Task AddAsync(NotificationJob job, CancellationToken cancellationToken = default);
    Task<NotificationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationJob>> GetDueAsync(DateTime before, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid habitId, NotificationChannel channel, DateTime scheduledAt, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
