using System.Net;
using System.Text.Json;
using RajMudra.Application.Common.Exceptions;

namespace RajMudra.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        string? code = null;

        switch (exception)
        {
            case ValidationException:
                statusCode = HttpStatusCode.BadRequest;
                code = "validation_error";
                break;
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                code = "not_found";
                break;
            case ForbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                code = "forbidden";
                break;
        }

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled domain exception");
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error = new
            {
                code = code ?? "internal_error",
                message = exception.Message
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

