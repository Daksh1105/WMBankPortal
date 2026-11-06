using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Infrastructure.Identity;

namespace WMBankPortal.Infrastructure.Persistence;

/// <summary>
/// Primary Entity Framework Core database context for the Banking Portal.
/// Inherits from IdentityDbContext with Guid keys and implements IAppDbContext.
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RefreshToken entity
        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            b.Property(x => x.ReplacedByTokenHash)
                .HasMaxLength(128);

            b.Property(x => x.CreatedByIp)
                .HasMaxLength(64);

            b.Property(x => x.RevokedReason)
                .HasMaxLength(256);

            // Indexes for fast lookup and integrity
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.FamilyId);

            // Foreign key to AspNetUsers table
            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
