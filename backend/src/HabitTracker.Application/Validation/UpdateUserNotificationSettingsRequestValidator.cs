using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;

namespace HabitTracker.Application.Validation;

public sealed class UpdateUserNotificationSettingsRequestValidator : IInputValidator<UpdateUserNotificationSettingsRequest>
{
    public void Validate(UpdateUserNotificationSettingsRequest value)
    {
        var failures = new List<ValidationFailure>();

        if (!TimeOnly.TryParse(value.DefaultReminderTime, out _))
        {
            failures.Add(new ValidationFailure(nameof(value.DefaultReminderTime), "Default reminder time must have HH:mm format"));
        }

        failures.ThrowIfInvalid();
    }
}
