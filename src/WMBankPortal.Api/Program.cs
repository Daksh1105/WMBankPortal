using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;
using WMBankPortal.Api.Middlewares;
using WMBankPortal.Api.Services;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Infrastructure.Common;

// 1. Initialize bootstrap Serilog logger
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WMBankPortal.Api bootstrap initialization...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. Wire Serilog into host
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // 3. Register Core Services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    // Register Common Infrastructure / Application contracts
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

    // 4. Register Health Checks
    builder.Services.AddHealthChecks();

    // 5. Configure Swagger (Enabled strictly in Development)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Wealth Management Banking Portal API",
                Version = "v1",
                Description = "Secure Enterprise Banking Customer Portal & AI Support Assistant API."
            });
        });
    }

    // 6. Configure CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "https://localhost:5173"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Required for SameSite=Strict / credentials handling
        });
    });

    // 7. Configure Rate Limiting (Phase 0 Baseline)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // General sliding window rate limiter for all standard API calls
        options.AddSlidingWindowLimiter("api-limiter", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 6;
            opt.QueueLimit = 0;
        });

        // Strict fixed window limiter for sensitive auth endpoints (prevent brute-force)
        options.AddFixedWindowLimiter("auth-limiter", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
    });

    var app = builder.Build();

    // =========================================================================
    // STRICT MIDDLEWARE PIPELINE ORDER (Per Enterprise Banking Architecture)
    // =========================================================================

    // 1. Global Exception Handler (RFC 7807 ProblemDetails; never leaks stack traces)
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 2. HTTPS Redirection & HSTS in non-development (avoids warning when running local HTTP profile)
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // 3. Security Headers (CSP, X-Frame-Options, X-Content-Type-Options)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 4. CORS Policy
    app.UseCors("DefaultCorsPolicy");

    // 5. Rate Limiter
    app.UseRateLimiter();

    // 6. Routing
    app.UseRouting();

    // 7. Authentication (Placeholder - wired fully in Phase 1 with Identity & JWT)
    app.UseAuthentication();

    // 8. Authorization
    app.UseAuthorization();

    // 9. Correlation ID and Request Tracing Middleware
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 10. Swagger UI (strictly Development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "WM Bank Portal API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // 11. Endpoint mappings
    app.MapControllers().RequireRateLimiting("api-limiter");
    app.MapHealthChecks("/health");

    // Root redirect to Swagger UI in Development for instant developer feedback
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    Log.Information("WMBankPortal.Api initialized successfully.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "WMBankPortal.Api terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

