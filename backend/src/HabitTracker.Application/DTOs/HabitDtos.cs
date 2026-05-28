namespace HabitTracker.Application.DTOs;

public sealed record CreateHabitRequest(
    Guid UserId,
    string Title,
    string Description,
    string ReminderTime,
    bool NotifyInTelegram,
    bool NotifyByEmail,
    string Category = "general");

public sealed record UpdateHabitRequest(
    string Title,
    string Description,
    string ReminderTime,
    bool NotifyInTelegram,
    bool NotifyByEmail,
    string Category,
    bool IsActive);

public sealed record HabitResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    string Category,
    string ReminderTime,
    bool NotifyInTelegram,
    bool NotifyByEmail,
    bool IsActive,
    bool IsArchived,
    DateOnly CreatedAt,
    DateOnly? ArchivedAt);

public sealed record HabitListItemResponse(
    Guid Id,
    string Title,
    string Description,
    string Category,
    int CurrentStreak,
    bool IsCompletedToday,
    bool IsActive,
    bool IsArchived);

public sealed record CompleteHabitRequest(DateOnly? Date);

public sealed record HabitCompletionResponse(Guid HabitId, DateOnly Date, int CurrentStreak);

public sealed record HabitCompletionDayResponse(DateOnly Date, bool IsCompleted);

public sealed record HabitCalendarResponse(
    Guid HabitId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<HabitCompletionDayResponse> Days);

public sealed record HabitStatsResponse(
    Guid HabitId,
    string Period,
    int CurrentStreak,
    int BestStreak,
    decimal CompletionRate,
    int CompletedDays,
    int MissedDays,
    int TotalTrackedDays,
    IReadOnlyList<DateOnly> CompletedDates);

public sealed record HabitExportResponse(string FileName, string ContentType, string Content);
