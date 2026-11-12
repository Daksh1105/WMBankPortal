using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Notifications.Services;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Domain.Exceptions;

namespace WMBankPortal.Application.Features.Underwriting.Services;

public class OfficerService : IOfficerService
{
    private readonly IAppDbContext _context;
    private readonly IAuditService _audit;
    private readonly INotificationService _notificationService;

    public OfficerService(
        IAppDbContext context, 
        IAuditService audit, 
        INotificationService notificationService)
    {
        _context = context;
        _audit = audit;
        _notificationService = notificationService;
    }

    public async Task<Result> ClaimApplicationAsync(Guid applicationId, Guid officerId, CancellationToken ct = default)
    {
        var app = await _context.BankingApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app == null) return Result.Failure(Error.NotFound("Application.NotFound", "Application not found."));

        try
        {
            app.Claim(officerId);
            
            _audit.Record("ApplicationClaimed", nameof(BankingApplication), app.Id.ToString(), new { applicationId = app.Id });
            await _context.SaveChangesAsync(ct);
            
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(Error.Conflict("Application.InvalidTransition", ex.Message)); }
    }

    public async Task<Result> ApproveApplicationAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default)
    {
        var app = await _context.BankingApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app == null) return Result.Failure(Error.NotFound("Application.NotFound", "Application not found."));

        try
        {
            app.Approve(officerId, remarks);
            
            _audit.Record("ApplicationApproved", nameof(BankingApplication), app.Id.ToString(), new { applicationId = app.Id });
            await _context.SaveChangesAsync(ct);

            await _notificationService.CreateAndSendAsync(
                app.CustomerId, 
                "Application Approved", 
                $"Congratulations! Your application {app.ApplicationNumber} has been approved.", 
                NotificationType.Success, 
                app.Id, 
                ct);

            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(Error.Conflict("Application.InvalidTransition", ex.Message)); }
    }

    public async Task<Result> RejectApplicationAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default)
    {
        var app = await _context.BankingApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app == null) return Result.Failure(Error.NotFound("Application.NotFound", "Application not found."));

        try
        {
            app.Reject(officerId, remarks);
            
            _audit.Record("ApplicationRejected", nameof(BankingApplication), app.Id.ToString(), new { applicationId = app.Id });
            await _context.SaveChangesAsync(ct);

            await _notificationService.CreateAndSendAsync(
                app.CustomerId, 
                "Application Rejected", 
                $"Your application {app.ApplicationNumber} has been declined. Remarks: {remarks}", 
                NotificationType.Warning, 
                app.Id, 
                ct);

            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(Error.Conflict("Application.InvalidTransition", ex.Message)); }
    }

    public async Task<Result> RequestKycAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default)
    {
        var app = await _context.BankingApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app == null) return Result.Failure(Error.NotFound("Application.NotFound", "Application not found."));

        try
        {
            app.RequestKyc(officerId, remarks);
            
            _audit.Record("ApplicationKycRequested", nameof(BankingApplication), app.Id.ToString(), new { applicationId = app.Id });
            await _context.SaveChangesAsync(ct);

            await _notificationService.CreateAndSendAsync(
                app.CustomerId, 
                "Action Required: KYC Missing", 
                $"Your application {app.ApplicationNumber} requires additional KYC documents. Remarks: {remarks}", 
                NotificationType.ActionRequired, 
                app.Id, 
                ct);

            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(Error.Conflict("Application.InvalidTransition", ex.Message)); }
    }
}