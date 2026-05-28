using HabitTracker.Api.Contracts;
using HabitTracker.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<HabitStore>();
builder.Services.AddSingleton<HabitStatsService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = error is ArgumentException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        await Results.Problem(error?.Message ?? "Unexpected server error", statusCode: context.Response.StatusCode).ExecuteAsync(context);
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Habit Tracker API v1");
    options.RoutePrefix = "swagger";
});

var store = app.Services.GetRequiredService<HabitStore>();
await store.InitializeAsync();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .WithTags("System");

app.MapGet("/health", async (HabitStore habitStore, CancellationToken ct) =>
{
    var snapshot = await habitStore.SnapshotAsync(ct);
    return Results.Ok(new
    {
        status = "ok",
        users = snapshot.Users.Count,
        habits = snapshot.Habits.Count,
        completions = snapshot.Completions.Count,
        utc = DateTime.UtcNow
    });
})
.WithName("Health")
.WithTags("System");

app.MapPost("/api/demo/reset", async (HabitStore habitStore, CancellationToken ct) =>
{
    var snapshot = await habitStore.ResetDemoAsync(ct);
    return Results.Ok(new { status = "reset", users = snapshot.Users.Count, habits = snapshot.Habits.Count });
})
.WithTags("System");

app.MapGet("/api/users", async (HabitStore habitStore, CancellationToken ct) =>
{
    var users = await habitStore.GetUsersAsync(ct);
    return Results.Ok(users);
})
.WithName("GetUsers")
.WithTags("Users");

app.MapGet("/api/users/{id:guid}", async (Guid id, HabitStore habitStore, CancellationToken ct) =>
{
    var user = await habitStore.GetUserAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.WithName("GetUser")
.WithTags("Users");

app.MapGet("/api/users/by-telegram/{telegramUserId:long}", async (long telegramUserId, HabitStore habitStore, CancellationToken ct) =>
{
    var user = await habitStore.GetUserByTelegramIdAsync(telegramUserId, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.WithName("GetUserByTelegramId")
.WithTags("Users");

app.MapPost("/api/users", async (CreateUserRequest request, HabitStore habitStore, CancellationToken ct) =>
{
    var user = await habitStore.CreateUserAsync(request, ct);
    return Results.Created($"/api/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithTags("Users");

app.MapGet("/api/users/{userId:guid}/habits", async (Guid userId, bool? includeArchived, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var snapshot = await habitStore.SnapshotAsync(ct);
    if (snapshot.Users.All(x => x.Id != userId))
    {
        return Results.NotFound(new { message = "User not found" });
    }

    var habits = snapshot.Habits
        .Where(x => x.UserId == userId && ((includeArchived ?? false) || !x.IsArchived))
        .OrderBy(x => x.Title)
        .Select(x => stats.ToHabitResponse(x, snapshot.Completions))
        .ToArray();
    return Results.Ok(habits);
})
.WithName("GetHabits")
.WithTags("Habits");

app.MapGet("/api/habits/{habitId:guid}", async (Guid habitId, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var snapshot = await habitStore.SnapshotAsync(ct);
    var habit = snapshot.Habits.FirstOrDefault(x => x.Id == habitId);
    return habit is null ? Results.NotFound() : Results.Ok(stats.ToHabitResponse(habit, snapshot.Completions));
})
.WithName("GetHabit")
.WithTags("Habits");

app.MapPost("/api/users/{userId:guid}/habits", async (Guid userId, CreateHabitRequest request, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var habit = await habitStore.CreateHabitAsync(userId, request, ct);
    if (habit is null)
    {
        return Results.NotFound(new { message = "User not found" });
    }

    return Results.Created($"/api/habits/{habit.Id}", stats.ToHabitResponse(habit, []));
})
.WithName("CreateHabit")
.WithTags("Habits");

app.MapPut("/api/habits/{habitId:guid}", async (Guid habitId, UpdateHabitRequest request, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var habit = await habitStore.UpdateHabitAsync(habitId, request, ct);
    return habit is null ? Results.NotFound() : Results.Ok(stats.ToHabitResponse(habit, []));
})
.WithName("UpdateHabit")
.WithTags("Habits");

app.MapPatch("/api/habits/{habitId:guid}/archive", async (Guid habitId, bool isArchived, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var habit = await habitStore.ArchiveHabitAsync(habitId, isArchived, ct);
    return habit is null ? Results.NotFound() : Results.Ok(stats.ToHabitResponse(habit, []));
})
.WithName("ArchiveHabit")
.WithTags("Habits");

app.MapDelete("/api/habits/{habitId:guid}", async (Guid habitId, HabitStore habitStore, CancellationToken ct) =>
{
    var removed = await habitStore.DeleteHabitAsync(habitId, ct);
    return removed ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteHabit")
.WithTags("Habits");

app.MapPost("/api/habits/{habitId:guid}/completions", async (Guid habitId, CompleteHabitRequest request, HabitStore habitStore, CancellationToken ct) =>
{
    var completion = await habitStore.CompleteHabitAsync(habitId, request, ct);
    return completion is null ? Results.NotFound(new { message = "Active habit not found" }) : Results.Created($"/api/habits/{habitId}/completions/{completion.Date:yyyy-MM-dd}", completion);
})
.WithName("CompleteHabit")
.WithTags("Completions");

app.MapGet("/api/habits/{habitId:guid}/completions", async (Guid habitId, DateOnly? from, DateOnly? to, HabitStore habitStore, CancellationToken ct) =>
{
    var completions = await habitStore.GetCompletionsAsync(habitId, from, to, ct);
    return completions is null ? Results.NotFound() : Results.Ok(completions);
})
.WithName("GetCompletions")
.WithTags("Completions");

app.MapDelete("/api/habits/{habitId:guid}/completions/{date}", async (Guid habitId, DateOnly date, HabitStore habitStore, CancellationToken ct) =>
{
    var removed = await habitStore.RemoveCompletionAsync(habitId, date, ct);
    return removed ? Results.NoContent() : Results.NotFound();
})
.WithName("RemoveCompletion")
.WithTags("Completions");

app.MapGet("/api/users/{userId:guid}/stats", async (Guid userId, DateOnly? from, DateOnly? to, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var snapshot = await habitStore.SnapshotAsync(ct);
    if (snapshot.Users.All(x => x.Id != userId))
    {
        return Results.NotFound(new { message = "User not found" });
    }

    var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var start = from ?? end.AddDays(-29);
    if (end < start)
    {
        return Results.BadRequest(new { message = "Parameter 'to' must be greater than or equal to 'from'." });
    }

    return Results.Ok(stats.BuildUserStats(snapshot, userId, start, end));
})
.WithName("GetUserStats")
.WithTags("Stats");

app.MapGet("/api/reminders/due", async (TimeOnly? time, DateOnly? day, HabitStore habitStore, HabitStatsService stats, CancellationToken ct) =>
{
    var actualTime = time ?? TimeOnly.FromDateTime(DateTime.UtcNow);
    var actualDay = day ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var snapshot = await habitStore.SnapshotAsync(ct);
    return Results.Ok(stats.BuildDueReminders(snapshot, new TimeOnly(actualTime.Hour, actualTime.Minute), actualDay));
})
.WithName("GetDueReminders")
.WithTags("Reminders");

app.Run();

public partial class Program;
