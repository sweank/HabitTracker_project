using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("api/habits")]
public sealed class HabitsController : ControllerBase
{
    private readonly IHabitService _habits;

    public HabitsController(IHabitService habits)
    {
        _habits = habits;
    }

    [HttpPost]
    public async Task<ActionResult<HabitResponse>> Create(CreateHabitRequest request, CancellationToken cancellationToken)
    {
        var habit = await _habits.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStats), new { habitId = habit.Id }, habit);
    }

    [HttpPost("{habitId:guid}/completions")]
    public async Task<ActionResult<HabitCompletionResponse>> Complete(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _habits.CompleteAsync(habitId, request, cancellationToken));
    }

    [HttpGet("{habitId:guid}/stats")]
    public async Task<ActionResult<HabitStatsResponse>> GetStats(Guid habitId, CancellationToken cancellationToken)
    {
        return Ok(await _habits.GetStatsAsync(habitId, cancellationToken));
    }
}
