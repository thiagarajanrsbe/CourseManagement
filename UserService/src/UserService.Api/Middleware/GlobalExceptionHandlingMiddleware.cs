using System.Net;
using Serilog;
using Common.Exceptions;
using UnauthorizedAccessException = Common.Exceptions.UnauthorizedAccessException;

namespace UserService.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {            
            DuplicateException duplicateEx => new
            {
                statusCode = (int)HttpStatusCode.Conflict,
                success = false,
                message = duplicateEx.Message,
                errors = new Dictionary<string, string[]>()
            },
            NotFoundException notFoundEx => new
            {
                statusCode = (int)HttpStatusCode.NotFound,
                success = false,
                message = notFoundEx.Message,
                errors = new Dictionary<string, string[]>()
            },
            UnauthorizedAccessException unauthorizedEx => new
            {
                statusCode = (int)HttpStatusCode.Unauthorized,
                success = false,
                message = unauthorizedEx.Message,
                errors = new Dictionary<string, string[]>()
            },
            _ => new
            {
                statusCode = (int)HttpStatusCode.InternalServerError,
                success = false,
                message = "An internal server error occurred.",
                errors = new Dictionary<string, string[]>()
            }
        };

        context.Response.StatusCode = response.statusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}
