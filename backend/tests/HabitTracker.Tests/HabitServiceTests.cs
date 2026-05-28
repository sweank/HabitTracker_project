using FluentAssertions;
using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Application.Services;
using HabitTracker.Domain.Entities;
using Xunit;

namespace HabitTracker.Tests;

public sealed class HabitServiceTests
{
    [Fact]
    public async Task CompleteAsync_ShouldRejectDuplicateCompletionForSameDate()
    {
        var user = new User { Name = "Ivan", Email = "ivan@mail.com", TelegramChatId = "1" };
        var habit = new Habit { UserId = user.Id, Title = "Drink water", Category = "health", ReminderTime = new TimeOnly(9, 0), IsActive = true, CreatedAt = new DateOnly(2026, 5, 1) };
        var service = CreateService(user, habit);

        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        var act = () => service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == "HABIT_ALREADY_COMPLETED_TODAY");
    }

    [Fact]
    public async Task UndoCompletionAsync_ShouldRemoveExistingCompletionAndRecalculateStreak()
    {
        var user = new User { Name = "Ivan", Email = "ivan@mail.com", TelegramChatId = "1" };
        var habit = new Habit { UserId = user.Id, Title = "Walk", Category = "health", ReminderTime = new TimeOnly(9, 0), IsActive = true, CreatedAt = new DateOnly(2026, 5, 1) };
        var completions = new FakeCompletionRepository();
        var service = CreateService(user, habit, completions);

        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 17)));
        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        var result = await service.UndoCompletionAsync(habit.Id, new DateOnly(2026, 5, 18));

        result.CurrentStreak.Should().Be(1);
        (await completions.ExistsAsync(habit.Id, new DateOnly(2026, 5, 18))).Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCompletionRateForWeek()
    {
        var user = new User { Name = "Ivan", Email = "ivan@mail.com", TelegramChatId = "1" };
        var habit = new Habit { UserId = user.Id, Title = "Reading", Category = "study", ReminderTime = new TimeOnly(9, 0), IsActive = true, CreatedAt = new DateOnly(2026, 5, 12) };
        var completions = new FakeCompletionRepository();
        var service = CreateService(user, habit, completions);

        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 12)));
        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 14)));
        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        var stats = await service.GetStatsAsync(habit.Id, "week");

        stats.Period.Should().Be("week");
        stats.TotalTrackedDays.Should().Be(7);
        stats.CompletedDays.Should().Be(3);
        stats.MissedDays.Should().Be(4);
        stats.CompletionRate.Should().Be(42.9m);
    }

    private static HabitService CreateService(User user, Habit habit, FakeCompletionRepository? completions = null)
    {
        var users = new FakeUserRepository(user);
        var habits = new FakeHabitRepository(habit);
        var completionRepository = completions ?? new FakeCompletionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var notifications = new FakeNotificationJobService();
        var streakCalculator = new StreakCalculator();
        return new HabitService(
            users,
            habits,
            completionRepository,
            notifications,
            streakCalculator,
            new CompletionStatisticsCalculator(streakCalculator),
            new FixedDateTimeProvider(),
            unitOfWork,
            new NoopValidator<CreateHabitRequest>(),
            new NoopValidator<UpdateHabitRequest>());
    }

    private sealed class NoopValidator<T> : IInputValidator<T>
    {
        public void Validate(T value)
        {
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 5, 18);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;
        public FakeUserRepository(User user) => _user = user;
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.Id == id ? _user : null);
        public Task<User?> GetByTelegramChatIdAsync(string telegramChatId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.TelegramChatId == telegramChatId ? _user : null);
    }

    private sealed class FakeHabitRepository : IHabitRepository
    {
        private readonly Habit _habit;
        public FakeHabitRepository(Habit habit) => _habit = habit;
        public Task AddAsync(Habit habit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Habit habit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Habit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Habit?>(_habit.Id == id ? _habit : null);
        public Task<IReadOnlyList<Habit>> GetUserHabitsAsync(Guid userId, string? category = null, bool includeArchived = false, CancellationToken cancellationToken = default)
        {
            var matchCategory = string.IsNullOrWhiteSpace(category) || _habit.Category == category;
            var matchArchive = includeArchived || (_habit.IsActive && _habit.ArchivedAt == null);
            return Task.FromResult<IReadOnlyList<Habit>>(userId == _habit.UserId && matchCategory && matchArchive ? new[] { _habit } : Array.Empty<Habit>());
        }
        public Task<IReadOnlyList<Habit>> GetActiveHabitsWithUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Habit>>(new[] { _habit });
    }

    private sealed class FakeCompletionRepository : IHabitCompletionRepository
    {
        private readonly List<HabitCompletion> _items = new();
        public Task AddAsync(HabitCompletion completion, CancellationToken cancellationToken = default)
        {
            _items.Add(completion);
            return Task.CompletedTask;
        }
        public Task RemoveAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(x => x.HabitId == habitId && x.Date == date);
            return Task.CompletedTask;
        }
        public Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default) => Task.FromResult(_items.Any(x => x.HabitId == habitId && x.Date == date));
        public Task<IReadOnlyList<HabitCompletion>> GetByHabitIdAsync(Guid habitId, CancellationToken cancellationToken = default, DateOnly? from = null, DateOnly? to = null)
        {
            var query = _items.Where(x => x.HabitId == habitId);
            if (from.HasValue)
            {
                query = query.Where(x => x.Date >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(x => x.Date <= to.Value);
            }
            return Task.FromResult<IReadOnlyList<HabitCompletion>>(query.OrderBy(x => x.Date).ToList());
        }
    }

    private sealed class FakeNotificationJobService : INotificationJobService
    {
        public Task EnsureTodayJobsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<DueNotificationResponse>> GetDueAsync(DateTime before, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DueNotificationResponse>>(Array.Empty<DueNotificationResponse>());
        public Task MarkSentAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid jobId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CreateJobsForHabitAsync(Habit habit, User user, DateOnly date, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
