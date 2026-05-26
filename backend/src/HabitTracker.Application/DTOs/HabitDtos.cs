namespace HabitTracker.Application.DTOs;

public sealed record CreateHabitRequest(
    Guid UserId,
    string Title,
    string Description,
    string ReminderTime,
    bool NotifyInTelegram,
    bool NotifyByEmail);

public sealed record HabitResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    string ReminderTime,
    bool NotifyInTelegram,
    bool NotifyByEmail,
    bool IsActive);

public sealed record HabitListItemResponse(
    Guid Id,
    string Title,
    string Description,
    int CurrentStreak,
    bool IsCompletedToday);

public sealed record CompleteHabitRequest(DateOnly? Date);

public sealed record HabitCompletionResponse(Guid HabitId, DateOnly Date, int CurrentStreak);

public sealed record HabitStatsResponse(
    Guid HabitId,
    int CurrentStreak,
    int BestStreak,
    IReadOnlyList<DateOnly> CompletedDates);
