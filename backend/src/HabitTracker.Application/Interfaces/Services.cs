using HabitTracker.Application.DTOs;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByTelegramChatIdAsync(string telegramChatId, CancellationToken cancellationToken = default);
}

public interface IHabitService
{
    Task<HabitResponse> CreateAsync(CreateHabitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HabitListItemResponse>> GetUserHabitsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<HabitCompletionResponse> CompleteAsync(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken = default);
    Task<HabitStatsResponse> GetStatsAsync(Guid habitId, CancellationToken cancellationToken = default);
}

public interface INotificationJobService
{
    Task EnsureTodayJobsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DueNotificationResponse>> GetDueAsync(DateTime before, CancellationToken cancellationToken = default);
    Task MarkSentAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default);
    Task CreateJobsForHabitAsync(Habit habit, User user, DateOnly date, CancellationToken cancellationToken = default);
}

public interface IStreakCalculator
{
    int CalculateCurrentStreak(IEnumerable<DateOnly> completedDates, DateOnly today);
    int CalculateBestStreak(IEnumerable<DateOnly> completedDates);
}

public interface INotificationMessageFactory
{
    string BuildHabitReminder(Habit habit, User user);
    string BuildEmailSubject(Habit habit);
}
