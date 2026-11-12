using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using WMBankPortal.Api.Middlewares;
using WMBankPortal.Api.Hubs;
using WMBankPortal.Api.Services;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Auth.Validators;
using WMBankPortal.Application.Features.Profile.Services;
using WMBankPortal.Application.Features.Products.Services;
using WMBankPortal.Application.Features.Applications.Services;
using WMBankPortal.Application.Features.Kyc.Services;
using WMBankPortal.Application.Features.Notifications.Services;
using WMBankPortal.Infrastructure.Common;
using WMBankPortal.Infrastructure.Identity;
using WMBankPortal.Infrastructure.Notifications;
using WMBankPortal.Infrastructure.Persistence;
using WMBankPortal.Infrastructure.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using WMBankPortal.Application.Features.Underwriting.Services;

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

    // 3. Database & EF Core Configuration
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

    builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

    // 4. ASP.NET Core Identity Configuration
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Strict Banking Password Policy
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 10;

        // Account Lockout Policy (5 failed attempts = 15-minute lockout)
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User Policy
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // 5. JWT Authentication Configuration
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

    if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
        throw new InvalidOperationException(
            "JwtSettings:Secret is missing or shorter than 32 characters. Set it with dotnet user-secrets.");
    
    var tokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = tokenValidationParameters;

        // Handle WebSocket Query String Token Authentication for SignalR Hubs
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // 6. Register Application & Infrastructure Services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    // Forwarded headers config
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProfileService, ProfileService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IApplicationService, ApplicationService>();
    builder.Services.AddScoped<IOfficerService, OfficerService>();
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
    builder.Services.AddSingleton<IMalwareScanner, NoOpMalwareScanner>();
    builder.Services.AddScoped<IKycService, KycService>();
    builder.Services.AddSingleton<IEmailService, MockEmailService>();
    builder.Services.AddScoped<IRealTimeNotificationPublisher, SignalRNotificationPublisher>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IAuditService, WMBankPortal.Infrastructure.Auditing.AuditService>();

    // Register SignalR for real-time WebSocket notifications
    builder.Services.AddSignalR();

    // Register FluentValidation Validators
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // 7. Register Health Checks
    builder.Services.AddHealthChecks();

    // 8. Configure Swagger UI (Enabled strictly in Development)
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

    // 9. Configure CORS
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

    // 10. Configure Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        static string ClientKey(HttpContext ctx) =>
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var authLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
        var apiLimit = builder.Configuration.GetValue("RateLimiting:ApiPermitLimit", 100);

        // Applies to every request, per client IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx), _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = apiLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

        // Extra strict limit for auth endpoints, per client IP
        options.AddPolicy("auth-limiter", ctx =>
            RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });

    var app = builder.Build();

    // =========================================================================
    // STRICT MIDDLEWARE PIPELINE ORDER (Per Enterprise Banking Architecture)
    // =========================================================================

    app.UseForwardedHeaders();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseRouting();
    app.UseCors("DefaultCorsPolicy");
    app.UseRateLimiter();       // must come after UseRouting so endpoint attributes work
    app.UseAuthentication();
    app.UseAuthorization();

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
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health");

    // Root redirect to Swagger UI in Development for instant developer feedback
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    // =========================================================================
    // DEVELOPMENT DATABASE AUTOMIGRATION & SEEDING
    // =========================================================================
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            Log.Information("Applying EF Core database migrations to SQL Server...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully.");

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            await DatabaseSeeder.SeedDevelopmentDataAsync(dbContext, userManager, roleManager, app.Configuration, app.Logger);
            Log.Information("Development demo users, profiles, and banking products seeded successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during database migration or demo data seeding.");
        }
    }

    Log.Information("WMBankPortal.Api initialized successfully.");
    app.Run();
}
catch (HostAbortedException)
{
    // Required for EF Core design-time tools (migrations)
    throw;
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
