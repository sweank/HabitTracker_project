namespace HabitTracker.Domain.Entities;

public sealed class Habit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public TimeOnly ReminderTime { get; set; }
    public bool NotifyInTelegram { get; set; }
    public bool NotifyByEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ArchivedAt { get; set; }

    public List<HabitCompletion> Completions { get; set; } = new();
}
