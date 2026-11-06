using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Infrastructure.Identity;

namespace WMBankPortal.Infrastructure.Persistence;

/// <summary>
/// Seeds realistic demo user accounts and roles during Development only.
/// Reads demo passwords from configuration (User-Secrets in local dev).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // 1. Seed Roles
        var roles = Enum.GetNames<UserRole>();
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Seeded role: {Role}", roleName);
            }
        }

        // 2. Default demo password (can be overridden via user-secrets: DemoUsers:Password)
        var demoPassword = configuration["DemoUsers:Password"] ?? "BankDemo123!Secure#";

        // 3. Demo Users Map
        var demoUsers = new (string Email, string Role)[]
        {
            ("customer@wmportal.com", UserRole.Customer.ToString()),
            ("officer@wmportal.com", UserRole.Officer.ToString()),
            ("agent@wmportal.com", UserRole.SupportAgent.ToString()),
            ("admin@wmportal.com", UserRole.Admin.ToString())
        };

        foreach (var (email, role) in demoUsers)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, demoPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation("Seeded demo user: {Email} with role {Role}", email, role);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogWarning("Failed to seed user {Email}: {Errors}", email, errors);
                }
            }
        }
    }
}
