using Microsoft.AspNetCore.Identity;

namespace WMBankPortal.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user entity configured with Guid primary key.
/// Resides strictly in Infrastructure to keep Domain pure and package-free.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeactivated { get; set; }
}

/// <summary>
/// ASP.NET Core Identity role entity configured with Guid primary key.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
