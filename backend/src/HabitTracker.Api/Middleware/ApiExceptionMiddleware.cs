using System.Text.Json;
using HabitTracker.Application.Common;
using HabitTracker.Application.DTOs;

namespace HabitTracker.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.ErrorCode, exception.Message);
        }
        catch (BadHttpRequestException exception)
        {
            _logger.LogWarning(exception, "Invalid HTTP request");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "INVALID_HTTP_REQUEST", exception.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request was cancelled by client");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API error");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string errorCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new ErrorResponse(errorCode, message, context.TraceIdentifier), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
