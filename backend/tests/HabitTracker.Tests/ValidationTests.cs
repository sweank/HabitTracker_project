using FluentAssertions;
using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Validation;
using Xunit;

namespace HabitTracker.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void CreateUserRequestValidator_ShouldRejectEmptyName()
    {
        var validator = new CreateUserRequestValidator();

        var act = () => validator.Validate(new CreateUserRequest("", "ivan@mail.com", "123"));

        act.Should().Throw<AppException>()
            .Where(e => e.ErrorCode == "VALIDATION_ERROR" && e.Message.Contains("Name"));
    }

    [Fact]
    public void CreateHabitRequestValidator_ShouldRejectInvalidReminderTime()
    {
        var validator = new CreateHabitRequestValidator();

        var act = () => validator.Validate(new CreateHabitRequest(Guid.NewGuid(), "Water", "", "25:99", true, false));

        act.Should().Throw<AppException>()
            .Where(e => e.ErrorCode == "VALIDATION_ERROR" && e.Message.Contains("ReminderTime"));
    }
}
