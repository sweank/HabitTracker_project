namespace HabitTracker.Application.DTOs;

public sealed record DueNotificationResponse(
    Guid Id,
    Guid UserId,
    Guid HabitId,
    string Channel,
    string Recipient,
    string Text);

public sealed record NotificationFailedRequest(string Reason);
public sealed record NotificationStatusResponse(string Status);
