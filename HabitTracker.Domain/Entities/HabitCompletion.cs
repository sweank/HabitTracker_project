namespace HabitTracker.Domain.Entities;

public sealed class HabitCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HabitId { get; set; }
    public Habit? Habit { get; set; }
    public DateOnly Date { get; set; }
}
