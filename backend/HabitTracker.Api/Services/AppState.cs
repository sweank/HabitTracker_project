using HabitTracker.Api.Models;

namespace HabitTracker.Api.Services;

public sealed class AppState
{
    public List<AppUser> Users { get; set; } = [];
    public List<Habit> Habits { get; set; } = [];
    public List<HabitCompletion> Completions { get; set; } = [];
}
