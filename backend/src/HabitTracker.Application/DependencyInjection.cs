using HabitTracker.Application.Common;
using HabitTracker.Application.Interfaces;
using HabitTracker.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HabitTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IStreakCalculator, StreakCalculator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<INotificationJobService, NotificationJobService>();
        return services;
    }
}
