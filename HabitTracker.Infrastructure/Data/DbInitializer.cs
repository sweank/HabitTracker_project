using HabitTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var user = new User
        {
            Name = "Demo User",
            Email = "demo@example.local",
            TelegramChatId = "123456789"
        };

        var habit = new Habit
        {
            User = user,
            Title = "Drink water",
            Description = "Drink 2 liters of water",
            ReminderTime = new TimeOnly(9, 0),
            NotifyInTelegram = true,
            NotifyByEmail = true,
            IsActive = true
        };

        dbContext.Users.Add(user);
        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
