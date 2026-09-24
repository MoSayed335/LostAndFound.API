using System.Net;
using System.Text.Json;
using LostAndFound.Application.Common.Exceptions;

namespace LostAndFound.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => new
                {
                    field = FormatFieldName(kvp.Key),
                    message = msg
                }))
                .ToList();

            _logger.LogWarning("Validation failed for {Method} {Path}: {Count} error(s)", context.Request.Method, context.Request.Path, errors.Count);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, "Validation failed.", errors);
        }
        catch (BadHttpRequestException badRequestEx)
        {
            _logger.LogWarning(badRequestEx, "Malformed HTTP request for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, "Invalid request body or malformed payload.", Array.Empty<object>());
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "JSON parsing error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, "Invalid JSON payload format.", Array.Empty<object>());
        }
        catch (UnauthorizedAccessException unauthorizedEx)
        {
            _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteResponseAsync(context, HttpStatusCode.Unauthorized, "Unauthorized access.", Array.Empty<object>());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", Array.Empty<object>());
        }
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string message, object errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            success = false,
            message,
            errors
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string FormatFieldName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return string.Empty;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
