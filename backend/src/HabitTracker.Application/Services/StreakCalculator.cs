using HabitTracker.Application.Interfaces;

namespace HabitTracker.Application.Services;

public sealed class StreakCalculator : IStreakCalculator
{
    public int CalculateCurrentStreak(IEnumerable<DateOnly> completedDates, DateOnly today)
    {
        var dates = completedDates.ToHashSet();
        if (dates.Count == 0)
        {
            return 0;
        }

        var cursor = dates.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;

        while (dates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public int CalculateBestStreak(IEnumerable<DateOnly> completedDates)
    {
        var orderedDates = completedDates.Distinct().OrderBy(date => date).ToList();
        if (orderedDates.Count == 0)
        {
            return 0;
        }

        var best = 1;
        var current = 1;

        for (var i = 1; i < orderedDates.Count; i++)
        {
            if (orderedDates[i] == orderedDates[i - 1].AddDays(1))
            {
                current++;
            }
            else
            {
                current = 1;
            }

            best = Math.Max(best, current);
        }

        return best;
    }
}
