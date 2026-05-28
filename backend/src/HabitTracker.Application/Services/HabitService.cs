using System.Globalization;
using System.Text;
using System.Text.Json;
using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Services;

public sealed class HabitService : IHabitService
{
    private readonly IUserRepository _users;
    private readonly IHabitRepository _habits;
    private readonly IHabitCompletionRepository _completions;
    private readonly INotificationJobService _notificationJobs;
    private readonly IStreakCalculator _streakCalculator;
    private readonly ICompletionStatisticsCalculator _statisticsCalculator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInputValidator<CreateHabitRequest> _createHabitValidator;
    private readonly IInputValidator<UpdateHabitRequest> _updateHabitValidator;

    public HabitService(
        IUserRepository users,
        IHabitRepository habits,
        IHabitCompletionRepository completions,
        INotificationJobService notificationJobs,
        IStreakCalculator streakCalculator,
        ICompletionStatisticsCalculator statisticsCalculator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IInputValidator<CreateHabitRequest> createHabitValidator,
        IInputValidator<UpdateHabitRequest> updateHabitValidator)
    {
        _users = users;
        _habits = habits;
        _completions = completions;
        _notificationJobs = notificationJobs;
        _streakCalculator = streakCalculator;
        _statisticsCalculator = statisticsCalculator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _createHabitValidator = createHabitValidator;
        _updateHabitValidator = updateHabitValidator;
    }

    public async Task<HabitResponse> CreateAsync(CreateHabitRequest request, CancellationToken cancellationToken = default)
    {
        _createHabitValidator.Validate(request);

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "User was not found");

        var reminderTime = TimeOnly.Parse(request.ReminderTime);

        var habit = new Habit
        {
            UserId = user.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category = NormalizeCategory(request.Category),
            ReminderTime = reminderTime,
            NotifyInTelegram = request.NotifyInTelegram,
            NotifyByEmail = request.NotifyByEmail,
            IsActive = true,
            CreatedAt = _dateTimeProvider.Today
        };

        await _habits.AddAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationJobs.CreateJobsForHabitAsync(habit, user, _dateTimeProvider.Today, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(habit);
    }

    public async Task<HabitResponse> UpdateAsync(Guid habitId, UpdateHabitRequest request, CancellationToken cancellationToken = default)
    {
        _updateHabitValidator.Validate(request);

        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        habit.Title = request.Title.Trim();
        habit.Description = request.Description?.Trim() ?? string.Empty;
        habit.Category = NormalizeCategory(request.Category);
        habit.ReminderTime = TimeOnly.Parse(request.ReminderTime);
        habit.NotifyInTelegram = request.NotifyInTelegram;
        habit.NotifyByEmail = request.NotifyByEmail;
        habit.IsActive = request.IsActive;
        if (habit.IsActive)
        {
            habit.ArchivedAt = null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(habit);
    }

    public async Task DeleteAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        await _habits.DeleteAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<HabitResponse> ArchiveAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        habit.IsActive = false;
        habit.ArchivedAt = _dateTimeProvider.Today;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(habit);
    }

    public async Task<IReadOnlyList<HabitListItemResponse>> GetUserHabitsAsync(Guid userId, string? category = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "User was not found");

        var habits = await _habits.GetUserHabitsAsync(user.Id, NormalizeCategoryFilter(category), includeArchived, cancellationToken);
        var result = new List<HabitListItemResponse>();

        foreach (var habit in habits)
        {
            var completions = await _completions.GetByHabitIdAsync(habit.Id, cancellationToken);
            var dates = completions.Select(c => c.Date).ToList();
            result.Add(new HabitListItemResponse(
                habit.Id,
                habit.Title,
                habit.Description,
                habit.Category,
                _streakCalculator.CalculateCurrentStreak(dates, _dateTimeProvider.Today),
                dates.Contains(_dateTimeProvider.Today),
                habit.IsActive,
                habit.ArchivedAt.HasValue));
        }

        return result;
    }

    public async Task<HabitCompletionResponse> CompleteAsync(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        if (!habit.IsActive)
        {
            throw AppException.BadRequest("HABIT_ARCHIVED", "Archived habits cannot be completed");
        }

        var date = request.Date ?? _dateTimeProvider.Today;

        if (await _completions.ExistsAsync(habitId, date, cancellationToken))
        {
            throw AppException.Conflict("HABIT_ALREADY_COMPLETED_TODAY", "Habit is already completed for this date");
        }

        var completion = new HabitCompletion
        {
            HabitId = habit.Id,
            Date = date
        };

        await _completions.AddAsync(completion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken);
        var currentStreak = _streakCalculator.CalculateCurrentStreak(completions.Select(c => c.Date), _dateTimeProvider.Today);
        return new HabitCompletionResponse(habit.Id, date, currentStreak);
    }

    public async Task<HabitCompletionResponse> UndoCompletionAsync(Guid habitId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        _ = await _habits.GetByIdAsync(habitId, cancellationToken)
            ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var targetDate = date ?? _dateTimeProvider.Today;
        if (!await _completions.ExistsAsync(habitId, targetDate, cancellationToken))
        {
            throw AppException.NotFound("HABIT_COMPLETION_NOT_FOUND", "Completion was not found for this date");
        }

        await _completions.RemoveAsync(habitId, targetDate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken);
        var currentStreak = _streakCalculator.CalculateCurrentStreak(completions.Select(c => c.Date), _dateTimeProvider.Today);
        return new HabitCompletionResponse(habitId, targetDate, currentStreak);
    }

    public async Task<IReadOnlyList<DateOnly>> GetCompletionHistoryAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        _ = await _habits.GetByIdAsync(habitId, cancellationToken)
            ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken);
        return completions.Select(c => c.Date).OrderBy(date => date).ToList();
    }

    public async Task<HabitCalendarResponse> GetCalendarAsync(Guid habitId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var rangeTo = to ?? _dateTimeProvider.Today;
        var rangeFrom = from ?? rangeTo.AddDays(-29);
        if (rangeTo < rangeFrom)
        {
            throw AppException.BadRequest("INVALID_DATE_RANGE", "Date range end must be greater than or equal to start");
        }

        if (rangeFrom < habit.CreatedAt)
        {
            rangeFrom = habit.CreatedAt;
        }

        if (rangeTo < rangeFrom)
        {
            return new HabitCalendarResponse(habitId, rangeFrom, rangeTo, Array.Empty<HabitCompletionDayResponse>());
        }

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken, rangeFrom, rangeTo);
        var completed = completions.Select(c => c.Date).ToHashSet();
        var days = new List<HabitCompletionDayResponse>();
        for (var cursor = rangeFrom; cursor <= rangeTo; cursor = cursor.AddDays(1))
        {
            days.Add(new HabitCompletionDayResponse(cursor, completed.Contains(cursor)));
        }

        return new HabitCalendarResponse(habitId, rangeFrom, rangeTo, days);
    }

    public async Task<HabitStatsResponse> GetStatsAsync(Guid habitId, string? period = null, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken);
        var dates = completions.Select(c => c.Date).OrderBy(d => d).ToList();
        var resolvedPeriod = ResolveStatsPeriod(period, habit.CreatedAt, _dateTimeProvider.Today);
        var statistics = _statisticsCalculator.Calculate(dates, resolvedPeriod.From, resolvedPeriod.To, _dateTimeProvider.Today);

        return new HabitStatsResponse(
            habitId,
            resolvedPeriod.Name,
            statistics.CurrentStreak,
            statistics.BestStreak,
            statistics.CompletionRate,
            statistics.CompletedDays,
            statistics.MissedDays,
            statistics.TotalTrackedDays,
            dates);
    }

    public async Task<HabitExportResponse> ExportHistoryAsync(Guid habitId, string format, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var calendar = await GetCalendarAsync(habitId, habit.CreatedAt, _dateTimeProvider.Today, cancellationToken);
        var normalizedFormat = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();

        if (normalizedFormat == "csv")
        {
            var builder = new StringBuilder();
            builder.AppendLine("date,isCompleted");
            foreach (var day in calendar.Days)
            {
                builder.AppendLine($"{day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)},{day.IsCompleted.ToString().ToLowerInvariant()}");
            }

            return new HabitExportResponse($"habit-{habit.Id}-history.csv", "text/csv; charset=utf-8", builder.ToString());
        }

        if (normalizedFormat == "json")
        {
            var payload = new
            {
                habit.Id,
                habit.Title,
                habit.Category,
                calendar.From,
                calendar.To,
                calendar.Days
            };
            var content = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            return new HabitExportResponse($"habit-{habit.Id}-history.json", "application/json; charset=utf-8", content);
        }

        throw AppException.BadRequest("UNSUPPORTED_EXPORT_FORMAT", "Supported export formats: csv, json");
    }

    private static HabitResponse ToResponse(Habit habit) => new(
        habit.Id,
        habit.UserId,
        habit.Title,
        habit.Description,
        habit.Category,
        habit.ReminderTime.ToString("HH:mm"),
        habit.NotifyInTelegram,
        habit.NotifyByEmail,
        habit.IsActive,
        habit.ArchivedAt.HasValue,
        habit.CreatedAt,
        habit.ArchivedAt);

    private static string NormalizeCategory(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? "general" : category.Trim().ToLowerInvariant();
    }

    private static string? NormalizeCategoryFilter(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToLowerInvariant();
    }

    private static StatsPeriod ResolveStatsPeriod(string? period, DateOnly createdAt, DateOnly today)
    {
        var normalized = string.IsNullOrWhiteSpace(period) ? "all" : period.Trim().ToLowerInvariant();
        DateOnly from;
        var name = normalized;

        switch (normalized)
        {
            case "week":
                from = today.AddDays(-6);
                break;
            case "month":
                from = new DateOnly(today.Year, today.Month, 1);
                break;
            case "all":
            case "alltime":
            case "all_time":
                from = createdAt;
                name = "all";
                break;
            default:
                throw AppException.BadRequest("INVALID_STATS_PERIOD", "Supported periods: week, month, all");
        }

        if (from < createdAt)
        {
            from = createdAt;
        }

        return new StatsPeriod(name, from, today);
    }

    private sealed record StatsPeriod(string Name, DateOnly From, DateOnly To);
}
