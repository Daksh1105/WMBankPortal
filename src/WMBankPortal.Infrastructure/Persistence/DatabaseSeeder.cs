using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Infrastructure.Identity;

namespace WMBankPortal.Infrastructure.Persistence;

/// <summary>
/// Seeds realistic demo user accounts, profiles, and banking products during Development.
/// Reads demo passwords from configuration (User-Secrets in local dev).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(
        AppDbContext dbContext,
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
        var demoPassword = configuration["DemoUsers:Password"];
        var seedUsers = !string.IsNullOrWhiteSpace(demoPassword);
        if (!seedUsers)
            logger.LogWarning("DemoUsers:Password is not set; skipping demo user seeding.");

        // 3. Demo Users Map
        var demoUsers = new (string Email, string Role)[]
        {
            ("customer@wmportal.com", UserRole.Customer.ToString()),
            ("officer@wmportal.com", UserRole.Officer.ToString()),
            ("agent@wmportal.com", UserRole.SupportAgent.ToString()),
            ("admin@wmportal.com", UserRole.Admin.ToString())
        };

        ApplicationUser? demoCustomerUser = null;

        foreach (var (email, role) in seedUsers ? demoUsers : Array.Empty<(string Email, string Role)>())
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

                var result = await userManager.CreateAsync(user, demoPassword!);
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

            if (email == "customer@wmportal.com")
            {
                demoCustomerUser = user;
            }
        }

        // 4. Seed Demo Customer Profile
        if (demoCustomerUser != null)
        {
            var hasProfile = await dbContext.CustomerProfiles.AnyAsync(p => p.UserId == demoCustomerUser.Id);
            if (!hasProfile)
            {
                var profile = new CustomerProfile(
                    demoCustomerUser.Id,
                    "Daksh",
                    "Gupta",
                    "+91 98765 43210",
                    new DateTime(1995, 5, 11),
                    "124 Marine Drive, Nariman Point",
                    "Mumbai",
                    "400021",
                    "India");

                dbContext.CustomerProfiles.Add(profile);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Seeded demo customer profile for: {Email}", demoCustomerUser.Email);
            }
        }

        // 5. Seed Predefined Banking Products with Dynamic JSON Schemas
        if (!await dbContext.BankingProducts.AnyAsync())
        {
            var products = new List<BankingProduct>
            {
                new BankingProduct(
                    code: "SAV-01",
                    name: "High-Yield Wealth Savings Account",
                    category: "Accounts",
                    interestRate: 4.50m,
                    minAmount: 10000.00m,
                    maxAmount: 10000000.00m,
                    dynamicFormSchemaJson: """
                    [
                      { "id": "initialDeposit", "label": "Initial Deposit Amount (INR)", "type": "number", "required": true, "min": 10000 },
                      { "id": "sourceOfFunds", "label": "Primary Source of Funds", "type": "select", "required": true, "options": ["Salary", "Business Revenue", "Investment Proceeds", "Inheritance"] },
                      { "id": "debitCardTier", "label": "Preferred Debit Card Tier", "type": "select", "required": true, "options": ["Platinum Visa", "Signature Mastercard", "World Elite"] },
                      { "id": "nomineeName", "label": "Nominee Full Legal Name", "type": "text", "required": true },
                      { "id": "nomineeRelationship", "label": "Nominee Relationship", "type": "select", "required": true, "options": ["Spouse", "Child", "Parent", "Sibling"] }
                    ]
                    """),

                new BankingProduct(
                    code: "CC-PLAT",
                    name: "Wealth Platinum Credit Card",
                    category: "Cards",
                    interestRate: 14.99m,
                    minAmount: 50000.00m,
                    maxAmount: 1500000.00m,
                    dynamicFormSchemaJson: """
                    [
                      { "id": "annualIncome", "label": "Annual Gross Income (INR)", "type": "number", "required": true, "min": 600000 },
                      { "id": "employmentType", "label": "Employment Type", "type": "select", "required": true, "options": ["Salaried Corporate", "Self-Employed Professional", "Business Owner"] },
                      { "id": "employerName", "label": "Current Employer / Business Entity", "type": "text", "required": true },
                      { "id": "designation", "label": "Designation / Role", "type": "text", "required": true },
                      { "id": "preferredLimit", "label": "Requested Credit Limit (INR)", "type": "number", "required": true, "min": 50000, "max": 1500000 }
                    ]
                    """),

                new BankingProduct(
                    code: "LOAN-PERS",
                    name: "Elite Personal & Investment Loan",
                    category: "Loans",
                    interestRate: 8.75m,
                    minAmount: 100000.00m,
                    maxAmount: 5000000.00m,
                    dynamicFormSchemaJson: """
                    [
                      { "id": "loanPurpose", "label": "Primary Purpose of Loan", "type": "select", "required": true, "options": ["Home Renovation", "Market Investment", "Debt Consolidation", "Higher Education"] },
                      { "id": "loanTenureMonths", "label": "Repayment Tenure (Months)", "type": "select", "required": true, "options": ["12", "24", "36", "48", "60"] },
                      { "id": "monthlyNetIncome", "label": "Monthly In-Hand Income (INR)", "type": "number", "required": true, "min": 50000 },
                      { "id": "existingMonthlyEmi", "label": "Existing Total Monthly EMIs (INR)", "type": "number", "required": true, "min": 0 }
                    ]
                    """),

                new BankingProduct(
                    code: "FD-GROWTH",
                    name: "Fixed Deposit Capital Growth Certificate",
                    category: "Deposits",
                    interestRate: 7.25m,
                    minAmount: 25000.00m,
                    maxAmount: 20000000.00m,
                    dynamicFormSchemaJson: """
                    [
                      { "id": "tenureMonths", "label": "Deposit Duration (Months)", "type": "select", "required": true, "options": ["6", "12", "24", "36", "60"] },
                      { "id": "interestPayout", "label": "Interest Payout Frequency", "type": "select", "required": true, "options": ["Cumulative (At Maturity)", "Quarterly Compounded", "Monthly Income"] },
                      { "id": "autoRenewal", "label": "Maturity Auto-Renewal", "type": "select", "required": true, "options": ["Yes (Principal + Interest)", "Yes (Principal Only)", "No"] },
                      { "id": "nomineeName", "label": "Nominee Full Legal Name", "type": "text", "required": true }
                    ]
                    """)
            };

            dbContext.BankingProducts.AddRange(products);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded 4 predefined banking products with dynamic form schemas.");
        }
    }
}
