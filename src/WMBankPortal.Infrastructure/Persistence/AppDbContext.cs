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
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<BankingProduct> BankingProducts => Set<BankingProduct>();
    public DbSet<BankingApplication> BankingApplications => Set<BankingApplication>();
    public DbSet<ApplicationTimeline> ApplicationTimelines => Set<ApplicationTimeline>();

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

        // Configure CustomerProfile entity (1:1 with AspNetUsers)
        builder.Entity<CustomerProfile>(b =>
        {
            b.ToTable("CustomerProfiles");
            b.HasKey(x => x.Id);

            b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
            b.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            b.Property(x => x.Country).HasMaxLength(100).IsRequired();

            b.HasIndex(x => x.UserId).IsUnique();

            b.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<CustomerProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BankingProduct entity
        builder.Entity<BankingProduct>(b =>
        {
            b.ToTable("BankingProducts");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(32).IsRequired();
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.InterestRate).HasColumnType("decimal(5,2)").IsRequired();
            b.Property(x => x.MinAmount).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.MaxAmount).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.DynamicFormSchemaJson).HasColumnType("nvarchar(max)").IsRequired();

            b.HasIndex(x => x.Code).IsUnique();
        });

        // Configure BankingApplication entity
        builder.Entity<BankingApplication>(b =>
        {
            b.ToTable("BankingApplications");
            b.HasKey(x => x.Id);

            b.Property(x => x.ApplicationNumber).HasMaxLength(32).IsRequired();
            b.Property(x => x.RequestedAmount).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.FormValuesJson).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.OfficerRemarks).HasMaxLength(1000);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasIndex(x => x.ApplicationNumber).IsUnique();
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);

            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne<BankingProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ReviewedByOfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Timelines)
                .WithOne()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationTimeline entity
        builder.Entity<ApplicationTimeline>(b =>
        {
            b.ToTable("ApplicationTimelines");
            b.HasKey(x => x.Id);

            b.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.ChangedByRole).HasMaxLength(64).IsRequired();
            b.Property(x => x.Comments).HasMaxLength(1000);

            b.HasIndex(x => x.ApplicationId);
            b.HasIndex(x => x.Timestamp);
        });
    }
}
