using FinanceTracker.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace FinanceTracker.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message, (object?)null),
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Message, (object?)null),
            ValidationException e => (HttpStatusCode.BadRequest, "Validation failed", (object?)e.Errors),
            ConflictException e => (HttpStatusCode.Conflict, e.Message, (object?)null),
            InvalidOperationException e => (HttpStatusCode.BadRequest, e.Message, (object?)null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            title,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}