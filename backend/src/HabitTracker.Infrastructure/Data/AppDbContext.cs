using HabitTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HabitTracker.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitCompletion> HabitCompletions => Set<HabitCompletion>();
    public DbSet<NotificationJob> NotificationJobs => Set<NotificationJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var timeOnlyConverter = new ValueConverter<TimeOnly, string>(
            value => value.ToString("HH:mm"),
            value => TimeOnly.Parse(value));

        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            value => value.ToString("yyyy-MM-dd"),
            value => DateOnly.Parse(value));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, string?>(
            value => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : null,
            value => string.IsNullOrWhiteSpace(value) ? null : DateOnly.Parse(value));

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TelegramChatId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.TelegramNotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.EmailNotificationsEnabled).HasDefaultValue(false);
            entity.Property(x => x.DefaultReminderTime)
                .HasConversion(timeOnlyConverter)
                .HasMaxLength(5);
            entity.HasIndex(x => x.TelegramChatId).IsUnique();
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ReminderTime)
                .HasConversion(timeOnlyConverter)
                .HasMaxLength(5);
            entity.Property(x => x.CreatedAt)
                .HasConversion(dateOnlyConverter)
                .HasMaxLength(10);
            entity.Property(x => x.ArchivedAt)
                .HasConversion(nullableDateOnlyConverter)
                .HasMaxLength(10);
            entity.HasIndex(x => new { x.UserId, x.Category });
            entity.HasOne(x => x.User)
                .WithMany(x => x.Habits)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitCompletion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date)
                .HasConversion(dateOnlyConverter)
                .HasMaxLength(10);
            entity.HasIndex(x => new { x.HabitId, x.Date }).IsUnique();
            entity.HasOne(x => x.Habit)
                .WithMany(x => x.Completions)
                .HasForeignKey(x => x.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.HabitId, x.Channel, x.ScheduledAt }).IsUnique();
        });
    }
}
