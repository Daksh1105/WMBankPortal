namespace WMBankPortal.Api.Middlewares;

/// <summary>
/// Injects mandatory defense-in-depth HTTP security headers into every response.
/// Compliant with OWASP Secure Headers guidelines for financial applications.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME-sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // Prevent Clickjacking
            headers.Append("X-Frame-Options", "DENY");

            // Disable outdated legacy browser XSS filters that introduced vulnerabilities
            headers.Append("X-XSS-Protection", "0");

            // Restrict referrer data leakage
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content Security Policy
            headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self' wss: ws:; frame-ancestors 'none';");

            // Permissions Policy
            headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
