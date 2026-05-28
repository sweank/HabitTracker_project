using HabitTracker.Api.Models;

namespace HabitTracker.Api.Contracts;

public sealed record HabitResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    HabitFrequency Frequency,
    int TargetCountPerPeriod,
    string? Color,
    TimeOnly? ReminderTime,
    bool IsArchived,
    DateTime CreatedAtUtc,
    int CurrentStreak,
    DateOnly? LastCompletedAt);

public sealed record HabitStatsResponse(
    Guid HabitId,
    string Title,
    int CompletedCount,
    int TargetCount,
    double CompletionRate,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastCompletedAt);

public sealed record UserStatsResponse(
    Guid UserId,
    DateOnly From,
    DateOnly To,
    int TotalHabits,
    int ActiveHabits,
    int CompletedMarks,
    IReadOnlyCollection<HabitStatsResponse> Habits);

public sealed record DueReminderResponse(
    Guid HabitId,
    Guid UserId,
    string Title,
    TimeOnly ReminderTime,
    DateOnly Day);
