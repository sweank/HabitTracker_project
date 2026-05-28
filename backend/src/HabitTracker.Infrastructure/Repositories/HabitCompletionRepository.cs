using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Infrastructure.Repositories;

public sealed class HabitCompletionRepository : IHabitCompletionRepository
{
    private readonly AppDbContext _dbContext;

    public HabitCompletionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(HabitCompletion completion, CancellationToken cancellationToken = default)
    {
        await _dbContext.HabitCompletions.AddAsync(completion, cancellationToken);
    }

    public async Task RemoveAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var completion = await _dbContext.HabitCompletions
            .FirstOrDefaultAsync(x => x.HabitId == habitId && x.Date == date, cancellationToken);
        if (completion is not null)
        {
            _dbContext.HabitCompletions.Remove(completion);
        }
    }

    public Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return _dbContext.HabitCompletions.AnyAsync(x => x.HabitId == habitId && x.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<HabitCompletion>> GetByHabitIdAsync(Guid habitId, CancellationToken cancellationToken = default, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _dbContext.HabitCompletions.Where(x => x.HabitId == habitId);

        if (from.HasValue)
        {
            query = query.Where(x => x.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Date <= to.Value);
        }

        return await query
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }
}
