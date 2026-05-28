using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;

namespace HabitTracker.Application.Validation;

public sealed class CreateUserRequestValidator : IInputValidator<CreateUserRequest>
{
    public void Validate(CreateUserRequest value)
    {
        var failures = new List<ValidationFailure>();

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            failures.Add(new ValidationFailure(nameof(value.Name), "User name is required"));
        }

        if (string.IsNullOrWhiteSpace(value.TelegramChatId))
        {
            failures.Add(new ValidationFailure(nameof(value.TelegramChatId), "Telegram chat id is required"));
        }

        if (!string.IsNullOrWhiteSpace(value.Email) && !value.Email.Contains('@'))
        {
            failures.Add(new ValidationFailure(nameof(value.Email), "Email must contain @"));
        }

        failures.ThrowIfInvalid();
    }
}
