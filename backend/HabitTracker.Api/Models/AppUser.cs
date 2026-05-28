namespace HabitTracker.Api.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long? TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
