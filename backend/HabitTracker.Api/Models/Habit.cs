namespace HabitTracker.Api.Models;

public sealed class Habit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    public int TargetCountPerPeriod { get; set; } = 1;
    public string? Color { get; set; }
    public TimeOnly? ReminderTime { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
