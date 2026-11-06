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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
