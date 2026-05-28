using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;

namespace HabitTracker.Application.Validation;

public sealed class UpdateHabitRequestValidator : IInputValidator<UpdateHabitRequest>
{
    public void Validate(UpdateHabitRequest value)
    {
        var failures = new List<ValidationFailure>();

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

        if (value.Category is { Length: > 80 })
        {
            failures.Add(new ValidationFailure(nameof(value.Category), "Category must be shorter than 80 characters"));
        }

        failures.ThrowIfInvalid();
    }
}
