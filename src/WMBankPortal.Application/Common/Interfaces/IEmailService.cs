using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Service abstraction for transactional email dispatches.
/// Implemented via Mock in development and SMTP/SendGrid/SES in production.
/// </summary>
public interface IEmailService
{
    Task<Result> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
