using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;

namespace HabitTracker.Application.Validation;

public sealed class CreateHabitRequestValidator : IInputValidator<CreateHabitRequest>
{
    public void Validate(CreateHabitRequest value)
    {
        var failures = new List<ValidationFailure>();

        if (value.UserId == Guid.Empty)
        {
            failures.Add(new ValidationFailure(nameof(value.UserId), "User id is required"));
        }

        if (string.IsNullOrWhiteSpace(value.Title))
        {
            failures.Add(new ValidationFailure(nameof(value.Title), "Habit title is required"));
        }

        if (value.Title is { Length: > 120 })
        {
            failures.Add(new ValidationFailure(nameof(value.Title), "Habit title must be shorter than 120 characters"));
        }

        if (!TimeOnly.TryParse(value.ReminderTime, out _))
        {
            failures.Add(new ValidationFailure(nameof(value.ReminderTime), "Reminder time must have HH:mm format"));
        }

        failures.ThrowIfInvalid();
    }
}
