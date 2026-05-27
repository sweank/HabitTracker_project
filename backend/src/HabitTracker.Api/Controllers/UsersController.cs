using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IHabitService _habits;

    public UsersController(IUserService users, IHabitService habits)
    {
        _users = users;
        _habits = habits;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByTelegram), new { telegramChatId = user.TelegramChatId }, user);
    }

    [HttpGet("by-telegram/{telegramChatId}")]
    public async Task<ActionResult<UserResponse>> GetByTelegram(string telegramChatId, CancellationToken cancellationToken)
    {
        return Ok(await _users.GetByTelegramChatIdAsync(telegramChatId, cancellationToken));
    }

    [HttpGet("{userId:guid}/habits")]
    public async Task<ActionResult<IReadOnlyList<HabitListItemResponse>>> GetHabits(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _habits.GetUserHabitsAsync(userId, cancellationToken));
    }
}
