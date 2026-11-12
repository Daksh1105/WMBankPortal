using Microsoft.Extensions.Logging;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Infrastructure.Notifications;

/// <summary>
/// Mock email dispatch service. Logs outgoing transactional emails to structured logger.
/// Zero cloud dependencies required for local execution, dev demo, and automated tests.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task<Result> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL DISPATCH] To: {ToEmail} | Subject: {Subject} | Body Length: {Length} chars",
            toEmail,
            subject,
            htmlBody?.Length ?? 0);

        return Task.FromResult(Result.Success());
    }
}
