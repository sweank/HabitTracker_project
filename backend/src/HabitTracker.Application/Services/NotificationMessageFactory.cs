using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Services;

public sealed class NotificationMessageFactory : INotificationMessageFactory
{
    public string BuildHabitReminder(Habit habit, User user)
    {
        return $"Не забудь выполнить привычку: {habit.Title}";
    }

    public string BuildEmailSubject(Habit habit)
    {
        return $"Habit Tracker: {habit.Title}";
    }
}
