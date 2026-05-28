using System.Text;
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

    [HttpPut("{habitId:guid}")]
    public async Task<ActionResult<HabitResponse>> Update(Guid habitId, UpdateHabitRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _habits.UpdateAsync(habitId, request, cancellationToken));
    }

    [HttpDelete("{habitId:guid}")]
    public async Task<IActionResult> Delete(Guid habitId, CancellationToken cancellationToken)
    {
        await _habits.DeleteAsync(habitId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{habitId:guid}/archive")]
    public async Task<ActionResult<HabitResponse>> Archive(Guid habitId, CancellationToken cancellationToken)
    {
        return Ok(await _habits.ArchiveAsync(habitId, cancellationToken));
    }

    [HttpPost("{habitId:guid}/completions")]
    public async Task<ActionResult<HabitCompletionResponse>> Complete(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _habits.CompleteAsync(habitId, request, cancellationToken));
    }

    [HttpDelete("{habitId:guid}/completions")]
    public async Task<ActionResult<HabitCompletionResponse>> UndoCompletion(Guid habitId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        return Ok(await _habits.UndoCompletionAsync(habitId, date, cancellationToken));
    }

    [HttpGet("{habitId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<DateOnly>>> GetHistory(Guid habitId, CancellationToken cancellationToken)
    {
        return Ok(await _habits.GetCompletionHistoryAsync(habitId, cancellationToken));
    }

    [HttpGet("{habitId:guid}/calendar")]
    public async Task<ActionResult<HabitCalendarResponse>> GetCalendar(
        Guid habitId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _habits.GetCalendarAsync(habitId, from, to, cancellationToken));
    }

    [HttpGet("{habitId:guid}/stats")]
    public async Task<ActionResult<HabitStatsResponse>> GetStats(Guid habitId, [FromQuery] string? period, CancellationToken cancellationToken)
    {
        return Ok(await _habits.GetStatsAsync(habitId, period, cancellationToken));
    }

    [HttpGet("{habitId:guid}/export")]
    public async Task<IActionResult> Export(Guid habitId, [FromQuery] string format, CancellationToken cancellationToken)
    {
        var export = await _habits.ExportHistoryAsync(habitId, format, cancellationToken);
        return File(Encoding.UTF8.GetBytes(export.Content), export.ContentType, export.FileName);
    }
}
