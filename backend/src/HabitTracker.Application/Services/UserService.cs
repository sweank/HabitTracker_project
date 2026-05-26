using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw AppException.BadRequest("INVALID_USER_NAME", "User name is required");
        }

        if (string.IsNullOrWhiteSpace(request.TelegramChatId))
        {
            throw AppException.BadRequest("INVALID_TELEGRAM_CHAT_ID", "Telegram chat id is required");
        }

        var existing = await _users.GetByTelegramChatIdAsync(request.TelegramChatId, cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            TelegramChatId = request.TelegramChatId.Trim()
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse> GetByTelegramChatIdAsync(string telegramChatId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByTelegramChatIdAsync(telegramChatId, cancellationToken)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "User was not found");

        return ToResponse(user);
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Name, user.Email, user.TelegramChatId);
}
