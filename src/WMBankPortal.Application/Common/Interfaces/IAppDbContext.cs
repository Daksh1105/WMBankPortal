using Microsoft.EntityFrameworkCore;
using WMBankPortal.Domain.Entities;

namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Application abstraction for the Entity Framework Core database context.
/// Exposes business DbSets without coupling Application to EF Core internals.
/// </summary>
public interface IAppDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<BankingProduct> BankingProducts { get; }
    DbSet<BankingApplication> BankingApplications { get; }
    DbSet<ApplicationTimeline> ApplicationTimelines { get; }
    DbSet<KycDocument> KycDocuments { get; }
    DbSet<UserNotification> UserNotifications { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
