using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("api/notification-jobs")]
public sealed class NotificationJobsController : ControllerBase
{
    private readonly INotificationJobService _notificationJobs;

    public NotificationJobsController(INotificationJobService notificationJobs)
    {
        _notificationJobs = notificationJobs;
    }

    [HttpGet("due")]
    public async Task<ActionResult<IReadOnlyList<DueNotificationResponse>>> GetDue([FromQuery] DateTime? before, CancellationToken cancellationToken)
    {
        var dueBefore = before ?? DateTime.UtcNow;
        return Ok(await _notificationJobs.GetDueAsync(dueBefore, cancellationToken));
    }

    [HttpPost("{jobId:guid}/sent")]
    public async Task<ActionResult<NotificationStatusResponse>> MarkSent(Guid jobId, CancellationToken cancellationToken)
    {
        await _notificationJobs.MarkSentAsync(jobId, cancellationToken);
        return Ok(new NotificationStatusResponse("sent"));
    }

    [HttpPost("{jobId:guid}/failed")]
    public async Task<ActionResult<NotificationStatusResponse>> MarkFailed(Guid jobId, NotificationFailedRequest request, CancellationToken cancellationToken)
    {
        await _notificationJobs.MarkFailedAsync(jobId, request.Reason, cancellationToken);
        return Ok(new NotificationStatusResponse("failed"));
    }
}
