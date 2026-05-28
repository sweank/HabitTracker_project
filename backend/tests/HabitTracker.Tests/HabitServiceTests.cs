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
        var habit = new Habit { UserId = user.Id, Title = "Drink water", ReminderTime = new TimeOnly(9, 0), IsActive = true };
        var users = new FakeUserRepository(user);
        var habits = new FakeHabitRepository(habit);
        var completions = new FakeCompletionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var notifications = new FakeNotificationJobService();
        var service = new HabitService(users, habits, completions, notifications, new StreakCalculator(), new FixedDateTimeProvider(), unitOfWork, new NoopValidator<CreateHabitRequest>());

        await service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        var act = () => service.CompleteAsync(habit.Id, new CompleteHabitRequest(new DateOnly(2026, 5, 18)));

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == "HABIT_ALREADY_COMPLETED_TODAY");
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
        public Task<Habit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Habit?>(_habit.Id == id ? _habit : null);
        public Task<IReadOnlyList<Habit>> GetUserHabitsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Habit>>(new[] { _habit });
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
        public Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default) => Task.FromResult(_items.Any(x => x.HabitId == habitId && x.Date == date));
        public Task<IReadOnlyList<HabitCompletion>> GetByHabitIdAsync(Guid habitId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<HabitCompletion>>(_items.Where(x => x.HabitId == habitId).ToList());
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
