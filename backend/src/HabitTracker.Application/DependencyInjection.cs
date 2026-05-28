using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;
using HabitTracker.Application.Interfaces;
using HabitTracker.Application.Services;
using HabitTracker.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HabitTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IStreakCalculator, StreakCalculator>();
        services.AddScoped<INotificationMessageFactory, NotificationMessageFactory>();
        services.AddScoped<IInputValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IInputValidator<CreateHabitRequest>, CreateHabitRequestValidator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<INotificationJobService, NotificationJobService>();
        return services;
    }
}
