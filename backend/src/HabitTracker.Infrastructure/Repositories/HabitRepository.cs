using HabitTracker.Application.Interfaces;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Infrastructure.Repositories;

public sealed class HabitRepository : IHabitRepository
{
    private readonly AppDbContext _dbContext;

    public HabitRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Habit habit, CancellationToken cancellationToken = default)
    {
        await _dbContext.Habits.AddAsync(habit, cancellationToken);
    }

    public Task DeleteAsync(Habit habit, CancellationToken cancellationToken = default)
    {
        _dbContext.Habits.Remove(habit);
        return Task.CompletedTask;
    }

    public Task<Habit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Habits.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetUserHabitsAsync(Guid userId, string? category = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Habits.AsQueryable().Where(x => x.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(x => x.IsActive && x.ArchivedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetActiveHabitsWithUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Habits
            .Include(x => x.User)
            .Where(x => x.IsActive && x.ArchivedAt == null)
            .ToListAsync(cancellationToken);
    }
}
