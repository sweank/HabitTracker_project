using HabitTracker.Api.Models;

namespace HabitTracker.Api.Contracts;

public sealed record CreateUserRequest(string Username, long? TelegramUserId);

public sealed record CreateHabitRequest(
    string Title,
    string? Description,
    HabitFrequency Frequency,
    int TargetCountPerPeriod,
    string? Color,
    TimeOnly? ReminderTime);

public sealed record UpdateHabitRequest(
    string Title,
    string? Description,
    HabitFrequency Frequency,
    int TargetCountPerPeriod,
    string? Color,
    TimeOnly? ReminderTime,
    bool IsArchived);

public sealed record CompleteHabitRequest(DateOnly? Date, string? Note);
