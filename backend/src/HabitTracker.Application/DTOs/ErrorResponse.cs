namespace HabitTracker.Application.DTOs;

public sealed record ErrorResponse(string ErrorCode, string Message, string TraceId);
