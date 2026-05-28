using HabitTracker.Application.Interfaces;

namespace HabitTracker.Application.Services;

public sealed class CompletionStatisticsCalculator : ICompletionStatisticsCalculator
{
    private readonly IStreakCalculator _streakCalculator;

    public CompletionStatisticsCalculator(IStreakCalculator streakCalculator)
    {
        _streakCalculator = streakCalculator;
    }

    public CompletionStatistics Calculate(IEnumerable<DateOnly> completedDates, DateOnly from, DateOnly to, DateOnly today)
    {
        if (to < from)
        {
            return new CompletionStatistics(0, 0, 0, 0, 0, 0);
        }

        var allCompletedDates = completedDates.Distinct().OrderBy(date => date).ToList();
        var completedInPeriod = allCompletedDates.Count(date => date >= from && date <= to);
        var totalTrackedDays = to.DayNumber - from.DayNumber + 1;
        var missedDays = Math.Max(0, totalTrackedDays - completedInPeriod);
        var completionRate = totalTrackedDays == 0
            ? 0
            : Math.Round((decimal)completedInPeriod / totalTrackedDays * 100, 1, MidpointRounding.AwayFromZero);

        return new CompletionStatistics(
            _streakCalculator.CalculateCurrentStreak(allCompletedDates, today),
            _streakCalculator.CalculateBestStreak(allCompletedDates),
            completionRate,
            completedInPeriod,
            missedDays,
            totalTrackedDays);
    }
}
