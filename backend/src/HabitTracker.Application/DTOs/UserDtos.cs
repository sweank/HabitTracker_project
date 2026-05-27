namespace HabitTracker.Application.DTOs;

public sealed record CreateUserRequest(string Name, string Email, string TelegramChatId);
public sealed record UserResponse(Guid Id, string Name, string Email, string TelegramChatId);
