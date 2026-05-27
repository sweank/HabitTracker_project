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

    public Task<Habit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Habits.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetUserHabitsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Habits
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetActiveHabitsWithUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Habits
            .Include(x => x.User)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }
}
