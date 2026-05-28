namespace HabitTracker.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;

    public bool NotificationsEnabled { get; set; } = true;
    public bool TelegramNotificationsEnabled { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = false;
    public TimeOnly DefaultReminderTime { get; set; } = new(9, 0);

    public List<Habit> Habits { get; set; } = new();
}
