namespace HabitTracker.Application.Common;

public sealed class AppException : Exception
{
    public AppException(string errorCode, string message, int statusCode) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }
    public int StatusCode { get; }

    public static AppException NotFound(string code, string message) => new(code, message, 404);
    public static AppException BadRequest(string code, string message) => new(code, message, 400);
    public static AppException Conflict(string code, string message) => new(code, message, 409);
}
