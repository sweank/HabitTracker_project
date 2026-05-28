using HabitTracker.Api.Contracts;
using HabitTracker.Api.Models;

namespace HabitTracker.Api.Services;

public sealed class HabitStatsService
{
    public HabitResponse ToHabitResponse(Habit habit, IEnumerable<HabitCompletion> completions)
    {
        var habitCompletions = completions.Where(x => x.HabitId == habit.Id).ToArray();
        return new HabitResponse(
            habit.Id,
            habit.UserId,
            habit.Title,
            habit.Description,
            habit.Frequency,
            habit.TargetCountPerPeriod,
            habit.Color,
            habit.ReminderTime,
            habit.IsArchived,
            habit.CreatedAtUtc,
            CalculateCurrentStreak(habit, habitCompletions),
            habitCompletions.OrderByDescending(x => x.Date).Select(x => (DateOnly?)x.Date).FirstOrDefault());
    }

    public UserStatsResponse BuildUserStats(AppState state, Guid userId, DateOnly from, DateOnly to)
    {
        var userHabits = state.Habits.Where(x => x.UserId == userId).ToArray();
        var stats = userHabits
            .OrderBy(x => x.Title)
            .Select(habit => BuildHabitStats(habit, state.Completions.Where(x => x.HabitId == habit.Id).ToArray(), from, to))
            .ToArray();

        return new UserStatsResponse(
            userId,
            from,
            to,
            userHabits.Length,
            userHabits.Count(x => !x.IsArchived),
            stats.Sum(x => x.CompletedCount),
            stats);
    }

    public IReadOnlyCollection<DueReminderResponse> BuildDueReminders(AppState state, TimeOnly time, DateOnly day)
    {
        return state.Habits
            .Where(habit => !habit.IsArchived && habit.ReminderTime == time)
            .Where(habit => state.Completions.All(x => x.HabitId != habit.Id || x.Date != day))
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.Title)
            .Select(habit => new DueReminderResponse(habit.Id, habit.UserId, habit.Title, time, day))
            .ToArray();
    }

    private HabitStatsResponse BuildHabitStats(Habit habit, IReadOnlyCollection<HabitCompletion> completions, DateOnly from, DateOnly to)
    {
        var inRange = completions.Where(x => x.Date >= from && x.Date <= to).ToArray();
        var target = CalculateTarget(habit, from, to);
        var completedCount = inRange.Length;
        var completionRate = target == 0 ? 0 : Math.Round((double)completedCount / target * 100, 2);

        return new HabitStatsResponse(
            habit.Id,
            habit.Title,
            completedCount,
            target,
            completionRate,
            CalculateCurrentStreak(habit, completions),
            CalculateLongestStreak(habit, completions),
            completions.OrderByDescending(x => x.Date).Select(x => (DateOnly?)x.Date).FirstOrDefault());
    }

    private static int CalculateTarget(Habit habit, DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return 0;
        }

        if (habit.Frequency == HabitFrequency.Daily)
        {
            return (to.DayNumber - from.DayNumber + 1) * habit.TargetCountPerPeriod;
        }

        var days = to.DayNumber - from.DayNumber + 1;
        var weeks = (int)Math.Ceiling(days / 7.0);
        return weeks * habit.TargetCountPerPeriod;
    }

    private static int CalculateCurrentStreak(Habit habit, IEnumerable<HabitCompletion> completions)
    {
        var completedDays = completions.Select(x => x.Date).Distinct().ToHashSet();
        if (completedDays.Count == 0)
        {
            return 0;
        }

        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!completedDays.Contains(day))
        {
            day = day.AddDays(-1);
        }

        var streak = 0;
        while (completedDays.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    private static int CalculateLongestStreak(Habit habit, IEnumerable<HabitCompletion> completions)
    {
        var days = completions.Select(x => x.Date).Distinct().OrderBy(x => x).ToArray();
        if (days.Length == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;
        for (var i = 1; i < days.Length; i++)
        {
            if (days[i].DayNumber == days[i - 1].DayNumber + 1)
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }
        return longest;
    }
}
