using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Infrastructure.Repositories;

public sealed class NotificationJobRepository : INotificationJobRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationJob job, CancellationToken cancellationToken = default)
    {
        await _dbContext.NotificationJobs.AddAsync(job, cancellationToken);
    }

    public Task<NotificationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.NotificationJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationJob>> GetDueAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationJobs
            .Where(x => x.Status == NotificationJobStatus.Pending && x.ScheduledAt <= before)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid habitId, NotificationChannel channel, DateTime scheduledAt, CancellationToken cancellationToken = default)
    {
        return _dbContext.NotificationJobs.AnyAsync(
            x => x.HabitId == habitId && x.Channel == channel && x.ScheduledAt == scheduledAt,
            cancellationToken);
    }
}
