using HabitTracker.Api.Services;
using Xunit;

namespace HabitTracker.Tests;

public sealed class HabitStatsServiceTests
{
    [Fact]
    public void BuildUserStats_ReturnsSeededDemoMetrics()
    {
        var state = SeedData.Create();
        var service = new HabitStatsService();
        var userId = state.Users.Single().Id;
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-6);

        var stats = service.BuildUserStats(state, userId, from, to);

        Assert.Equal(userId, stats.UserId);
        Assert.Equal(2, stats.TotalHabits);
        Assert.Equal(2, stats.ActiveHabits);
        Assert.True(stats.CompletedMarks >= 8);
        Assert.All(stats.Habits, habit => Assert.True(habit.TargetCount > 0));
    }
}
