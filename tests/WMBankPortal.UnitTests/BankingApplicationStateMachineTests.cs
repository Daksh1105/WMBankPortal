using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Domain.Exceptions;
using Xunit;

namespace WMBankPortal.UnitTests;

public class BankingApplicationStateMachineTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();
    private readonly Guid _otherCustomerId = Guid.NewGuid();

    [Fact]
    public void NewApplication_StartsInSubmittedState_WithInitialTimeline()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        Assert.Equal(ApplicationStatus.Submitted, app.Status);
        Assert.Single(app.Timelines);
        Assert.Equal(ApplicationStatus.Submitted, app.Timelines.First().ToStatus);
    }

    [Fact]
    public void Claim_FromSubmittedState_TransitionsToUnderReview()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);

        Assert.Equal(ApplicationStatus.UnderReview, app.Status);
        Assert.Equal(_officerId, app.ReviewedByOfficerId);
        Assert.Equal(2, app.Timelines.Count);
    }

    [Fact]
    public void Claim_FromNonSubmittedState_ThrowsInvalidStateTransitionException()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId); // Now UnderReview

        var ex = Assert.Throws<InvalidStateTransitionException>(() => app.Claim(_officerId));
        Assert.Equal("UnderReview", ex.FromState);
        Assert.Equal("UnderReview", ex.ToState);
    }

    [Fact]
    public void RequestKyc_FromUnderReview_WithRemarks_TransitionsToKycPending()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);
        app.RequestKyc(_officerId, "Please re-upload a clear government photo ID.");

        Assert.Equal(ApplicationStatus.KycPending, app.Status);
        Assert.Equal("Please re-upload a clear government photo ID.", app.OfficerRemarks);
        Assert.Equal(3, app.Timelines.Count);
    }

    [Fact]
    public void RequestKyc_WithoutRemarks_ThrowsInvalidStateTransitionException()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);

        Assert.Throws<InvalidStateTransitionException>(() => app.RequestKyc(_officerId, ""));
    }

    [Fact]
    public void Approve_FromUnderReview_TransitionsToApprovedTerminalState()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);
        app.Approve(_officerId, "Application meets all high-net-worth underwriting criteria.");

        Assert.Equal(ApplicationStatus.Approved, app.Status);
        Assert.NotNull(app.DecidedAt);
        Assert.Equal(3, app.Timelines.Count);

        // Cannot transition from Approved
        Assert.Throws<InvalidStateTransitionException>(() => app.Claim(_officerId));
    }

    [Fact]
    public void Reject_FromUnderReview_WithRemarks_TransitionsToRejectedTerminalState()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);
        app.Reject(_officerId, "Debt-to-income ratio exceeds regulatory threshold.");

        Assert.Equal(ApplicationStatus.Rejected, app.Status);
        Assert.NotNull(app.DecidedAt);
        Assert.Equal("Debt-to-income ratio exceeds regulatory threshold.", app.OfficerRemarks);

        // Cannot transition from Rejected
        Assert.Throws<InvalidStateTransitionException>(() => app.Approve(_officerId));
    }

    [Fact]
    public void ResubmitKyc_FromKycPending_TransitionsToUnderReview()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Claim(_officerId);
        app.RequestKyc(_officerId, "Missing address proof.");
        app.ResubmitKyc();

        Assert.Equal(ApplicationStatus.UnderReview, app.Status);
        Assert.Equal(4, app.Timelines.Count);
    }

    [Fact]
    public void Withdraw_ByCustomer_FromSubmittedState_TransitionsToWithdrawn()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        app.Withdraw(_customerId, "Found alternative terms.");

        Assert.Equal(ApplicationStatus.Withdrawn, app.Status);
        Assert.NotNull(app.DecidedAt);
    }

    [Fact]
    public void Withdraw_ByDifferentUser_ThrowsInvalidStateTransitionException()
    {
        var app = new BankingApplication(
            _customerId,
            Guid.NewGuid(),
            "APP-20261107-12345678",
            50000m,
            "{}");

        var ex = Assert.Throws<InvalidStateTransitionException>(() => app.Withdraw(_otherCustomerId));
        Assert.Contains("Only the customer who submitted the application can withdraw it", ex.Reason);
    }
}
