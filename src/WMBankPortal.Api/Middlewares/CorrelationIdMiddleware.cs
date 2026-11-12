using Serilog.Context;

namespace WMBankPortal.Api.Middlewares;

/// <summary>
/// Ensures every request has a unique correlation identifier for end-to-end distributed tracing.
/// Pushes CorrelationId into the Serilog diagnostic context.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = !string.IsNullOrEmpty(incoming)
                            && incoming.Length <= 64
                            && incoming.All(c => char.IsLetterOrDigit(c) || c == '-')
            ? incoming
            : Guid.NewGuid().ToString("N");
            
        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
