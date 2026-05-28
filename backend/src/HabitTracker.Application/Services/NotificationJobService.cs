using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Services;

public sealed class NotificationJobService : INotificationJobService
{
    private readonly IHabitRepository _habits;
    private readonly INotificationJobRepository _notificationJobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationMessageFactory _messageFactory;

    public NotificationJobService(
        IHabitRepository habits,
        INotificationJobRepository notificationJobs,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        INotificationMessageFactory messageFactory)
    {
        _habits = habits;
        _notificationJobs = notificationJobs;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _messageFactory = messageFactory;
    }

    public async Task EnsureTodayJobsAsync(CancellationToken cancellationToken = default)
    {
        var habits = await _habits.GetActiveHabitsWithUsersAsync(cancellationToken);
        foreach (var habit in habits)
        {
            if (habit.User is not null)
            {
                await CreateJobsForHabitAsync(habit, habit.User, _dateTimeProvider.Today, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateJobsForHabitAsync(Habit habit, User user, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (!habit.IsActive || !user.NotificationsEnabled)
        {
            return;
        }

        var scheduledAt = date.ToDateTime(habit.ReminderTime, DateTimeKind.Utc);
        var text = _messageFactory.BuildHabitReminder(habit, user);

        if (habit.NotifyInTelegram && user.TelegramNotificationsEnabled && !string.IsNullOrWhiteSpace(user.TelegramChatId))
        {
            await AddIfMissingAsync(habit, user, NotificationChannel.Telegram, user.TelegramChatId, text, scheduledAt, cancellationToken);
        }

        if (habit.NotifyByEmail && user.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(user.Email))
        {
            await AddIfMissingAsync(habit, user, NotificationChannel.Email, user.Email, text, scheduledAt, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<DueNotificationResponse>> GetDueAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        await EnsureTodayJobsAsync(cancellationToken);

        var jobs = await _notificationJobs.GetDueAsync(before, cancellationToken);
        return jobs.Select(job => new DueNotificationResponse(
            job.Id,
            job.UserId,
            job.HabitId,
            job.Channel.ToString().ToLowerInvariant(),
            job.Recipient,
            job.Text)).ToList();
    }

    public async Task MarkSentAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _notificationJobs.GetByIdAsync(jobId, cancellationToken)
                  ?? throw AppException.NotFound("NOTIFICATION_JOB_NOT_FOUND", "Notification job was not found");

        job.Status = NotificationJobStatus.Sent;
        job.FailureReason = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default)
    {
        var job = await _notificationJobs.GetByIdAsync(jobId, cancellationToken)
                  ?? throw AppException.NotFound("NOTIFICATION_JOB_NOT_FOUND", "Notification job was not found");

        job.Status = NotificationJobStatus.Failed;
        job.FailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown error" : reason;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task AddIfMissingAsync(
        Habit habit,
        User user,
        NotificationChannel channel,
        string recipient,
        string text,
        DateTime scheduledAt,
        CancellationToken cancellationToken)
    {
        if (await _notificationJobs.ExistsAsync(habit.Id, channel, scheduledAt, cancellationToken))
        {
            return;
        }

        await _notificationJobs.AddAsync(new NotificationJob
        {
            UserId = user.Id,
            HabitId = habit.Id,
            Channel = channel,
            Recipient = recipient,
            Text = text,
            ScheduledAt = scheduledAt,
            Status = NotificationJobStatus.Pending
        }, cancellationToken);
    }
}
