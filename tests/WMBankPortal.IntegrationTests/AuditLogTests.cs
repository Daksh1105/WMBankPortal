using Microsoft.EntityFrameworkCore;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Infrastructure.Persistence;
using Xunit;

namespace WMBankPortal.IntegrationTests;

public class AuditLogTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenModifyingAuditLog_ThrowsInvalidOperationException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=Dummy;Trusted_Connection=True;")
            .Options;

        await using var db = new AppDbContext(options);
        var log = new AuditLog(
            Guid.NewGuid(),
            "Customer",
            "TestAction",
            "TestEntity",
            "123",
            null,
            null,
            null,
            null);

        db.Attach(log);
        db.Entry(log).State = EntityState.Modified;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}