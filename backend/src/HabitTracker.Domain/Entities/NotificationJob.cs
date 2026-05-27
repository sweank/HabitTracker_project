namespace HabitTracker.Domain.Entities;

public sealed class NotificationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid HabitId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public NotificationJobStatus Status { get; set; } = NotificationJobStatus.Pending;
    public string? FailureReason { get; set; }
}

public enum NotificationChannel
{
    Telegram,
    Email
}

public enum NotificationJobStatus
{
    Pending,
    Sent,
    Failed
}
