using FluentAssertions;
using HabitTracker.Application.Services;
using Xunit;

namespace HabitTracker.Tests;

public sealed class CompletionStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnCompletedMissedAndCompletionRate()
    {
        var calculator = new CompletionStatisticsCalculator(new StreakCalculator());
        var today = new DateOnly(2026, 5, 18);

        var result = calculator.Calculate(
            [new DateOnly(2026, 5, 16), new DateOnly(2026, 5, 18)],
            new DateOnly(2026, 5, 15),
            today,
            today);

        result.TotalTrackedDays.Should().Be(4);
        result.CompletedDays.Should().Be(2);
        result.MissedDays.Should().Be(2);
        result.CompletionRate.Should().Be(50m);
        result.CurrentStreak.Should().Be(1);
    }
}
