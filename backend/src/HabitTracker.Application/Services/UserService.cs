using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInputValidator<CreateUserRequest> _createUserValidator;

    public UserService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IInputValidator<CreateUserRequest> createUserValidator)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _createUserValidator = createUserValidator;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _createUserValidator.Validate(request);

        var existing = await _users.GetByTelegramChatIdAsync(request.TelegramChatId, cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
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
