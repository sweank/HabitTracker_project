using HabitTracker.Application.DTOs;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByTelegramChatIdAsync(string telegramChatId, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateNotificationSettingsAsync(Guid userId, UpdateUserNotificationSettingsRequest request, CancellationToken cancellationToken = default);
}

public interface IHabitService
{
    Task<HabitResponse> CreateAsync(CreateHabitRequest request, CancellationToken cancellationToken = default);
    Task<HabitResponse> UpdateAsync(Guid habitId, UpdateHabitRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid habitId, CancellationToken cancellationToken = default);
    Task<HabitResponse> ArchiveAsync(Guid habitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HabitListItemResponse>> GetUserHabitsAsync(Guid userId, string? category = null, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<HabitCompletionResponse> CompleteAsync(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken = default);
    Task<HabitCompletionResponse> UndoCompletionAsync(Guid habitId, DateOnly? date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateOnly>> GetCompletionHistoryAsync(Guid habitId, CancellationToken cancellationToken = default);
    Task<HabitCalendarResponse> GetCalendarAsync(Guid habitId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<HabitStatsResponse> GetStatsAsync(Guid habitId, string? period = null, CancellationToken cancellationToken = default);
    Task<HabitExportResponse> ExportHistoryAsync(Guid habitId, string format, CancellationToken cancellationToken = default);
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

public sealed record CompletionStatistics(
    int CurrentStreak,
    int BestStreak,
    decimal CompletionRate,
    int CompletedDays,
    int MissedDays,
    int TotalTrackedDays);

public interface ICompletionStatisticsCalculator
{
    CompletionStatistics Calculate(IEnumerable<DateOnly> completedDates, DateOnly from, DateOnly to, DateOnly today);
}

public interface INotificationMessageFactory
{
    string BuildHabitReminder(Habit habit, User user);
    string BuildEmailSubject(Habit habit);
}
