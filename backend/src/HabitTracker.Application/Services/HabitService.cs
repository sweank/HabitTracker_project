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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public HabitService(
        IUserRepository users,
        IHabitRepository habits,
        IHabitCompletionRepository completions,
        INotificationJobService notificationJobs,
        IStreakCalculator streakCalculator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _habits = habits;
        _completions = completions;
        _notificationJobs = notificationJobs;
        _streakCalculator = streakCalculator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<HabitResponse> CreateAsync(CreateHabitRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "User was not found");

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw AppException.BadRequest("INVALID_HABIT_TITLE", "Habit title is required");
        }

        if (!TimeOnly.TryParse(request.ReminderTime, out var reminderTime))
        {
            throw AppException.BadRequest("INVALID_REMINDER_TIME", "Reminder time must have HH:mm format");
        }

        var habit = new Habit
        {
            UserId = user.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ReminderTime = reminderTime,
            NotifyInTelegram = request.NotifyInTelegram,
            NotifyByEmail = request.NotifyByEmail,
            IsActive = true
        };

        await _habits.AddAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationJobs.CreateJobsForHabitAsync(habit, user, _dateTimeProvider.Today, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(habit);
    }

    public async Task<IReadOnlyList<HabitListItemResponse>> GetUserHabitsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "User was not found");

        var habits = await _habits.GetUserHabitsAsync(user.Id, cancellationToken);
        var result = new List<HabitListItemResponse>();

        foreach (var habit in habits)
        {
            var completions = await _completions.GetByHabitIdAsync(habit.Id, cancellationToken);
            var dates = completions.Select(c => c.Date).ToList();
            result.Add(new HabitListItemResponse(
                habit.Id,
                habit.Title,
                habit.Description,
                _streakCalculator.CalculateCurrentStreak(dates, _dateTimeProvider.Today),
                dates.Contains(_dateTimeProvider.Today)));
        }

        return result;
    }

    public async Task<HabitCompletionResponse> CompleteAsync(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken = default)
    {
        var habit = await _habits.GetByIdAsync(habitId, cancellationToken)
                    ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

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

    public async Task<HabitStatsResponse> GetStatsAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        _ = await _habits.GetByIdAsync(habitId, cancellationToken)
            ?? throw AppException.NotFound("HABIT_NOT_FOUND", "Habit was not found");

        var completions = await _completions.GetByHabitIdAsync(habitId, cancellationToken);
        var dates = completions.Select(c => c.Date).OrderBy(d => d).ToList();

        return new HabitStatsResponse(
            habitId,
            _streakCalculator.CalculateCurrentStreak(dates, _dateTimeProvider.Today),
            _streakCalculator.CalculateBestStreak(dates),
            dates);
    }

    private static HabitResponse ToResponse(Habit habit) => new(
        habit.Id,
        habit.UserId,
        habit.Title,
        habit.Description,
        habit.ReminderTime.ToString("HH:mm"),
        habit.NotifyInTelegram,
        habit.NotifyByEmail,
        habit.IsActive);
}
