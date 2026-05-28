using System.Text.Json;
using HabitTracker.Api.Contracts;
using HabitTracker.Api.Models;

namespace HabitTracker.Api.Services;

public sealed class HabitStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private AppState _state = new();

    public HabitStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _filePath = configuration["DATA_FILE"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "habittracker.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (!File.Exists(_filePath))
        {
            _state = SeedData.Create();
            await SaveUnsafeAsync(cancellationToken);
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        _state = await JsonSerializer.DeserializeAsync<AppState>(stream, _jsonOptions, cancellationToken) ?? new AppState();
    }

    public async Task<AppUser> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var username = NormalizeRequired(request.Username, "Username");
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (request.TelegramUserId is not null)
            {
                var existing = _state.Users.FirstOrDefault(x => x.TelegramUserId == request.TelegramUserId);
                if (existing is not null)
                {
                    if (!string.Equals(existing.Username, username, StringComparison.Ordinal))
                    {
                        existing.Username = username;
                        await SaveUnsafeAsync(cancellationToken);
                    }
                    return existing;
                }
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                TelegramUserId = request.TelegramUserId,
                Username = username,
                CreatedAtUtc = DateTime.UtcNow
            };
            _state.Users.Add(user);
            await SaveUnsafeAsync(cancellationToken);
            return user;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyCollection<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _state.Users.OrderBy(x => x.CreatedAtUtc).ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<AppUser?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _state.Users.FirstOrDefault(x => x.Id == id);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<AppUser?> GetUserByTelegramIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _state.Users.FirstOrDefault(x => x.TelegramUserId == telegramUserId);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Habit?> CreateHabitAsync(Guid userId, CreateHabitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateHabit(request.Title, request.TargetCountPerPeriod);
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_state.Users.All(x => x.Id != userId))
            {
                return null;
            }

            var habit = new Habit
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title.Trim(),
                Description = NormalizeOptional(request.Description),
                Frequency = request.Frequency,
                TargetCountPerPeriod = request.TargetCountPerPeriod,
                Color = NormalizeOptional(request.Color),
                ReminderTime = request.ReminderTime,
                IsArchived = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            _state.Habits.Add(habit);
            await SaveUnsafeAsync(cancellationToken);
            return habit;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyCollection<Habit>> GetHabitsAsync(Guid userId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _state.Habits
                .Where(x => x.UserId == userId && (includeArchived || !x.IsArchived))
                .OrderBy(x => x.IsArchived)
                .ThenBy(x => x.Title)
                .ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Habit?> GetHabitAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _state.Habits.FirstOrDefault(x => x.Id == habitId);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Habit?> UpdateHabitAsync(Guid habitId, UpdateHabitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateHabit(request.Title, request.TargetCountPerPeriod);
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var habit = _state.Habits.FirstOrDefault(x => x.Id == habitId);
            if (habit is null)
            {
                return null;
            }

            habit.Title = request.Title.Trim();
            habit.Description = NormalizeOptional(request.Description);
            habit.Frequency = request.Frequency;
            habit.TargetCountPerPeriod = request.TargetCountPerPeriod;
            habit.Color = NormalizeOptional(request.Color);
            habit.ReminderTime = request.ReminderTime;
            habit.IsArchived = request.IsArchived;
            await SaveUnsafeAsync(cancellationToken);
            return habit;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Habit?> ArchiveHabitAsync(Guid habitId, bool isArchived, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var habit = _state.Habits.FirstOrDefault(x => x.Id == habitId);
            if (habit is null)
            {
                return null;
            }

            habit.IsArchived = isArchived;
            await SaveUnsafeAsync(cancellationToken);
            return habit;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> DeleteHabitAsync(Guid habitId, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var removed = _state.Habits.RemoveAll(x => x.Id == habitId) > 0;
            if (!removed)
            {
                return false;
            }

            _state.Completions.RemoveAll(x => x.HabitId == habitId);
            await SaveUnsafeAsync(cancellationToken);
            return true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<HabitCompletion?> CompleteHabitAsync(Guid habitId, CompleteHabitRequest request, CancellationToken cancellationToken = default)
    {
        var day = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var habit = _state.Habits.FirstOrDefault(x => x.Id == habitId && !x.IsArchived);
            if (habit is null)
            {
                return null;
            }

            var existing = _state.Completions.FirstOrDefault(x => x.HabitId == habitId && x.Date == day);
            if (existing is not null)
            {
                existing.Note = NormalizeOptional(request.Note);
                await SaveUnsafeAsync(cancellationToken);
                return existing;
            }

            var completion = new HabitCompletion
            {
                Id = Guid.NewGuid(),
                HabitId = habit.Id,
                UserId = habit.UserId,
                Date = day,
                Note = NormalizeOptional(request.Note),
                CreatedAtUtc = DateTime.UtcNow
            };
            _state.Completions.Add(completion);
            await SaveUnsafeAsync(cancellationToken);
            return completion;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyCollection<HabitCompletion>?> GetCompletionsAsync(Guid habitId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_state.Habits.All(x => x.Id != habitId))
            {
                return null;
            }

            var query = _state.Completions.Where(x => x.HabitId == habitId);
            if (from is not null)
            {
                query = query.Where(x => x.Date >= from.Value);
            }
            if (to is not null)
            {
                query = query.Where(x => x.Date <= to.Value);
            }

            return query.OrderByDescending(x => x.Date).ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> RemoveCompletionAsync(Guid habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var removed = _state.Completions.RemoveAll(x => x.HabitId == habitId && x.Date == date) > 0;
            if (removed)
            {
                await SaveUnsafeAsync(cancellationToken);
            }
            return removed;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<AppState> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return new AppState
            {
                Users = _state.Users.Select(CloneUser).ToList(),
                Habits = _state.Habits.Select(CloneHabit).ToList(),
                Completions = _state.Completions.Select(CloneCompletion).ToList()
            };
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<AppState> ResetDemoAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            _state = SeedData.Create();
            await SaveUnsafeAsync(cancellationToken);
            return _state;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task SaveUnsafeAsync(CancellationToken cancellationToken)
    {
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, _state, _jsonOptions, cancellationToken);
        }
        File.Move(tempPath, _filePath, true);
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{field} is required.");
        }
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateHabit(string title, int targetCountPerPeriod)
    {
        NormalizeRequired(title, nameof(title));
        if (targetCountPerPeriod < 1 || targetCountPerPeriod > 50)
        {
            throw new ArgumentException("TargetCountPerPeriod must be between 1 and 50.");
        }
    }

    private static AppUser CloneUser(AppUser x) => new()
    {
        Id = x.Id,
        TelegramUserId = x.TelegramUserId,
        Username = x.Username,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private static Habit CloneHabit(Habit x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        Title = x.Title,
        Description = x.Description,
        Frequency = x.Frequency,
        TargetCountPerPeriod = x.TargetCountPerPeriod,
        Color = x.Color,
        ReminderTime = x.ReminderTime,
        IsArchived = x.IsArchived,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private static HabitCompletion CloneCompletion(HabitCompletion x) => new()
    {
        Id = x.Id,
        HabitId = x.HabitId,
        UserId = x.UserId,
        Date = x.Date,
        Note = x.Note,
        CreatedAtUtc = x.CreatedAtUtc
    };
}
