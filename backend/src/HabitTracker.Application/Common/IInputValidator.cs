namespace HabitTracker.Application.Common;

public interface IInputValidator<in T>
{
    void Validate(T value);
}

public sealed record ValidationFailure(string Field, string Message);

public static class InputValidatorExtensions
{
    public static void ThrowIfInvalid(this IReadOnlyCollection<ValidationFailure> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        var message = string.Join("; ", failures.Select(x => $"{x.Field}: {x.Message}"));
        throw AppException.BadRequest("VALIDATION_ERROR", message);
    }
}
