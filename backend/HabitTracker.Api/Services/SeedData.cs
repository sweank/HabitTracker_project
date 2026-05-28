using HabitTracker.Api.Models;

namespace HabitTracker.Api.Services;

public static class SeedData
{
    public static AppState Create()
    {
        var user = new AppUser
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username = "demo",
            TelegramUserId = 100001,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };

        var water = new Habit
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = user.Id,
            Title = "Пить воду",
            Description = "Минимум 6 стаканов в день",
            Frequency = HabitFrequency.Daily,
            TargetCountPerPeriod = 1,
            Color = "blue",
            ReminderTime = new TimeOnly(9, 0),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-9)
        };

        var reading = new Habit
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = user.Id,
            Title = "Читать 20 минут",
            Description = "Ежедневное чтение",
            Frequency = HabitFrequency.Daily,
            TargetCountPerPeriod = 1,
            Color = "green",
            ReminderTime = new TimeOnly(21, 30),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var completions = new List<HabitCompletion>();
        for (var i = 0; i < 5; i++)
        {
            completions.Add(new HabitCompletion
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                HabitId = water.Id,
                Date = today.AddDays(-i),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }
        for (var i = 1; i < 4; i++)
        {
            completions.Add(new HabitCompletion
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                HabitId = reading.Id,
                Date = today.AddDays(-i),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        return new AppState
        {
            Users = [user],
            Habits = [water, reading],
            Completions = completions
        };
    }
}
