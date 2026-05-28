namespace HabitTracker.Application.DTOs;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string TelegramChatId,
    bool NotificationsEnabled = true,
    bool TelegramNotificationsEnabled = true,
    bool EmailNotificationsEnabled = false,
    string DefaultReminderTime = "09:00");

public sealed record UpdateUserNotificationSettingsRequest(
    bool NotificationsEnabled,
    bool TelegramNotificationsEnabled,
    bool EmailNotificationsEnabled,
    string DefaultReminderTime);

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string TelegramChatId,
    bool NotificationsEnabled,
    bool TelegramNotificationsEnabled,
    bool EmailNotificationsEnabled,
    string DefaultReminderTime);
