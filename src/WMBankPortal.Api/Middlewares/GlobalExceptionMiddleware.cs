using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Common.Exceptions;
using WMBankPortal.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace WMBankPortal.Api.Middlewares;

/// <summary>
/// Global exception handler intercepting all application exceptions.
/// Converts domain & application errors into standard RFC 7807 ProblemDetails JSON responses.
/// Prevents internal stack traces from leaking to client callers.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Failed",
                    Detail = valEx.Message,
                    Extensions = { ["errors"] = valEx.Errors }
                }),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message
                }),

            ForbiddenException forbiddenEx => (
                HttpStatusCode.Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden Access",
                    Detail = forbiddenEx.Message
                }),

            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Conflict Detected",
                    Detail = conflictEx.Message
                }),

            InvalidStateTransitionException stateEx => (
                HttpStatusCode.UnprocessableEntity,
                new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Invalid State Transition",
                    Detail = stateEx.Message,
                    Extensions =
                    {
                        ["entity"] = stateEx.EntityName,
                        ["fromState"] = stateEx.FromState,
                        ["toState"] = stateEx.ToState,
                        ["reason"] = stateEx.Reason
                    }
                }),

            DbUpdateConcurrencyException => (
            HttpStatusCode.Conflict,
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = "This record was changed by someone else. Reload and try again."
            }),
            
            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Detail = _environment.IsDevelopment()
                        ? exception.ToString()
                        : "An unexpected error occurred while processing your request. Please contact support."
                })
        };

        // Attach correlation identifier if present
        if (context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeader, out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
