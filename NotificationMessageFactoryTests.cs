using FluentAssertions;
using HabitTracker.Application.Services;
using HabitTracker.Domain.Entities;
using Xunit;

namespace HabitTracker.Tests;

public sealed class NotificationMessageFactoryTests
{
    [Fact]
    public void BuildHabitReminder_ShouldIncludeHabitTitle()
    {
        var factory = new NotificationMessageFactory();
        var habit = new Habit { Title = "Drink water" };
        var user = new User { Name = "Ivan" };

        var message = factory.BuildHabitReminder(habit, user);

        message.Should().Contain("Drink water");
    }

    [Fact]
    public void BuildEmailSubject_ShouldIncludeHabitTitle()
    {
        var factory = new NotificationMessageFactory();
        var habit = new Habit { Title = "Walk" };

        var subject = factory.BuildEmailSubject(habit);

        subject.Should().Contain("Walk");
    }
}
