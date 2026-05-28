using FluentAssertions;
using HabitTracker.Application.Services;
using Xunit;

namespace HabitTracker.Tests;

public sealed class StreakCalculatorTests
{
    private readonly StreakCalculator _calculator = new();
    private readonly DateOnly _today = new(2026, 5, 18);

    [Fact]
    public void CurrentStreak_ShouldBeZero_WhenNoCompletions()
    {
        _calculator.CalculateCurrentStreak([], _today).Should().Be(0);
    }

    [Fact]
    public void CurrentStreak_ShouldBeOne_WhenOnlyTodayCompleted()
    {
        _calculator.CalculateCurrentStreak([_today], _today).Should().Be(1);
    }

    [Fact]
    public void CurrentStreak_ShouldGrow_WhenSeveralDaysInRowCompleted()
    {
        var dates = new[] { _today.AddDays(-2), _today.AddDays(-1), _today };

        _calculator.CalculateCurrentStreak(dates, _today).Should().Be(3);
    }

    [Fact]
    public void CurrentStreak_ShouldReset_WhenThereIsGap()
    {
        var dates = new[] { _today.AddDays(-4), _today.AddDays(-3), _today };

        _calculator.CalculateCurrentStreak(dates, _today).Should().Be(1);
    }

    [Fact]
    public void BestStreak_ShouldReturnLongestSequence()
    {
        var dates = new[]
        {
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 5, 12),
            new DateOnly(2026, 5, 13),
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 18)
        };

        _calculator.CalculateBestStreak(dates).Should().Be(3);
    }
}
