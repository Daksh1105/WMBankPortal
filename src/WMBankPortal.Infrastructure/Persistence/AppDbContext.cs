using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Domain.Common;
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
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        // Configure KycDocument entity
        builder.Entity<KycDocument>(b =>
        {
            b.ToTable("KycDocuments");
            b.HasKey(x => x.Id);

            b.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.StorageBlobPath).HasMaxLength(255).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.ScanStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);

            b.HasIndex(x => x.ApplicationId);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);

            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne<BankingApplication>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.VerifiedByOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UserNotification entity
        builder.Entity<UserNotification>(b =>
        {
            b.ToTable("UserNotifications");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.IsRead).IsRequired();

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.UserId, x.IsRead });

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<BankingApplication>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AuditLog entity (append-only, no FK to users on purpose)
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();

            b.Property(x => x.UserRole).HasMaxLength(64);
            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityId).HasMaxLength(64);
            b.Property(x => x.DetailsJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.UserAgent).HasMaxLength(256);
            b.Property(x => x.CorrelationId).HasMaxLength(64);

            b.HasIndex(x => x.TimestampUtc);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CorrelationId);
            b.HasIndex(x => new { x.EntityName, x.EntityId });
        });

        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit rows are append-only: block any attempt to edit or delete one.
        foreach (var entry in ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("AuditLog entries are append-only and cannot be modified or deleted.");
            }
        }

        // Auto-populate UpdatedAtUtc for modified BaseEntity records
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
