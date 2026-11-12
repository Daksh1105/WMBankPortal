using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Notifications.Services;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Domain.Exceptions;
using WMBankPortal.Application.Features.Applications.Validators;

namespace WMBankPortal.Application.Features.Applications.Services;

public class ApplicationService : IApplicationService
{
    private readonly IAppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _audit;

    public ApplicationService(
        IAppDbContext context,
        INotificationService notificationService,
        IAuditService audit)
    {
        _context = context;
        _notificationService = notificationService;
        _audit = audit;
    }

    public async Task<Result<BankingApplicationDto>> SubmitApplicationAsync(
        Guid customerId,
        SubmitApplicationRequest request,
        CancellationToken ct = default)
    {
        var product = await _context.BankingProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, ct);

        if (product == null)
        {
            return Result<BankingApplicationDto>.Failure(
                Error.NotFound("Product.NotFound", "The selected banking product was not found or is inactive."));
        }

        if (request.RequestedAmount < product.MinAmount || request.RequestedAmount > product.MaxAmount)
        {
            return Result<BankingApplicationDto>.Failure(
                Error.Validation("Application.InvalidAmount",
                    $"Requested amount of {request.RequestedAmount:N2} must be between {product.MinAmount:N2} and {product.MaxAmount:N2} for {product.Name}."));
        }

        var formError = DynamicFormValidator.Validate(product.DynamicFormSchemaJson, request.FormValuesJson);
        if (formError != null)
        {
            return Result<BankingApplicationDto>.Failure(Error.Validation("Application.InvalidForm", formError));
        }

        // Retry loop for unique constraint on ApplicationNumber
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var randomHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
            var appNumber = $"APP-{DateTime.UtcNow:yyyyMMdd}-{randomHex}";

            var application = new BankingApplication(
                customerId,
                product.Id,
                appNumber,
                request.RequestedAmount,
                request.FormValuesJson);

            _context.BankingApplications.Add(application);

            try
            {
                await _context.SaveChangesAsync(ct);

                // Dispatch notification
                await _notificationService.CreateAndSendAsync(
                    customerId,
                    "Application Submitted",
                    $"Your application {application.ApplicationNumber} has been received and is queued for underwriting review.",
                    NotificationType.Success,
                    application.Id,
                    ct);

                return Result<BankingApplicationDto>.Success(MapToDto(application, product));
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                _context.BankingApplications.Remove(application);
            }
        }

        return Result<BankingApplicationDto>.Failure(
            Error.Failure("Application.NumberGenerationFailed", "Unable to generate unique application number. Please try again."));
    }

    public async Task<Result<PagedResult<BankingApplicationDto>>> GetMyApplicationsAsync(
        Guid customerId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.BankingApplications
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId);

        var totalCount = await query.CountAsync(ct);

        var items = await (
            from a in query
            join p in _context.BankingProducts.AsNoTracking() on a.ProductId equals p.Id
            orderby a.SubmittedAt descending
            select new { App = a, Product = p }
        )
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

        var dtos = items.Select(x => MapToDto(x.App, x.Product)).ToList();

        return Result<PagedResult<BankingApplicationDto>>.Success(
            new PagedResult<BankingApplicationDto>(dtos, totalCount, pageNumber, pageSize));
    }

    public async Task<Result<BankingApplicationDto>> GetApplicationByIdAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default)
    {
        var appWithProduct = await (
            from a in _context.BankingApplications.AsNoTracking()
            where a.Id == applicationId
            join p in _context.BankingProducts.AsNoTracking() on a.ProductId equals p.Id
            select new { App = a, Product = p }
        ).FirstOrDefaultAsync(ct);

        if (appWithProduct == null)
        {
            return Result<BankingApplicationDto>.Failure(
                Error.NotFound("Application.NotFound", $"Banking application with ID '{applicationId}' was not found."));
        }

        // IDOR Defense: Customers can ONLY see their own applications
        if (currentUserRole == UserRole.Customer.ToString() && appWithProduct.App.CustomerId != currentUserId)
        {
            return Result<BankingApplicationDto>.Failure(
                Error.Forbidden("Application.Forbidden", "You do not have permission to view this banking application."));
        }

        return Result<BankingApplicationDto>.Success(MapToDto(appWithProduct.App, appWithProduct.Product));
    }

    public async Task<Result<IReadOnlyList<ApplicationTimelineDto>>> GetApplicationTimelineAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default)
    {
        var app = await _context.BankingApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app == null)
        {
            return Result<IReadOnlyList<ApplicationTimelineDto>>.Failure(
                Error.NotFound("Application.NotFound", $"Banking application with ID '{applicationId}' was not found."));
        }

        // IDOR Defense
        if (currentUserRole == UserRole.Customer.ToString() && app.CustomerId != currentUserId)
        {
            return Result<IReadOnlyList<ApplicationTimelineDto>>.Failure(
                Error.Forbidden("Application.Forbidden", "You do not have permission to view this application's timeline."));
        }

        IReadOnlyList<ApplicationTimelineDto> timelines = await _context.ApplicationTimelines
            .AsNoTracking()
            .Where(t => t.ApplicationId == applicationId)
            .OrderBy(t => t.Timestamp)
            .Select(t => new ApplicationTimelineDto(
                t.Id,
                t.FromStatus.ToString(),
                t.ToStatus.ToString(),
                t.ChangedByRole,
                t.Comments,
                t.Timestamp))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ApplicationTimelineDto>>.Success(timelines);
    }

    public async Task<Result> WithdrawApplicationAsync(
        Guid applicationId,
        Guid customerId,
        WithdrawApplicationRequest request,
        CancellationToken ct = default)
    {
        var app = await _context.BankingApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app == null)
        {
            return Result.Failure(
                Error.NotFound("Application.NotFound", $"Banking application with ID '{applicationId}' was not found."));
        }

        // IDOR Defense
        if (app.CustomerId != customerId)
        {
            return Result.Failure(
                Error.Forbidden("Application.Forbidden", "You can only withdraw your own applications."));
        }

        try
        {
            app.Withdraw(customerId, request.Remarks);
            _audit.Record(
                "ApplicationWithdrawn",
                nameof(BankingApplication),
                app.Id.ToString(),
                new { applicationId = app.Id });
            await _context.SaveChangesAsync(ct);

            await _notificationService.CreateAndSendAsync(
                customerId,
                "Application Withdrawn",
                $"Your application {app.ApplicationNumber} has been successfully withdrawn.",
                NotificationType.Warning,
                app.Id,
                ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(
                Error.Conflict("Application.InvalidTransition", ex.Message));
        }
    }

    public async Task<Result<CustomerDashboardDto>> GetCustomerDashboardAsync(
        Guid customerId,
        CancellationToken ct = default)
    {
        var apps = await (
            from a in _context.BankingApplications.AsNoTracking()
            where a.CustomerId == customerId
            join p in _context.BankingProducts.AsNoTracking() on a.ProductId equals p.Id
            orderby a.SubmittedAt descending
            select new { App = a, Product = p }
        ).ToListAsync(ct);

        var total = apps.Count;
        var pending = apps.Count(x =>
            x.App.Status == ApplicationStatus.Submitted ||
            x.App.Status == ApplicationStatus.UnderReview ||
            x.App.Status == ApplicationStatus.KycPending);
        var approved = apps.Count(x => x.App.Status == ApplicationStatus.Approved);
        var rejected = apps.Count(x => x.App.Status == ApplicationStatus.Rejected);

        var recent = apps.Take(5).Select(x => MapToDto(x.App, x.Product)).ToList();

        return Result<CustomerDashboardDto>.Success(new CustomerDashboardDto(
            total,
            pending,
            approved,
            rejected,
            recent));
    }

    private static BankingApplicationDto MapToDto(BankingApplication a, BankingProduct p) =>
        new(
            a.Id,
            a.ApplicationNumber,
            a.CustomerId,
            a.ProductId,
            p.Name,
            p.Category,
            a.RequestedAmount,
            a.Status.ToString(),
            a.FormValuesJson,
            a.OfficerRemarks,
            a.SubmittedAt,
            a.DecidedAt,
            a.RowVersion);
}
