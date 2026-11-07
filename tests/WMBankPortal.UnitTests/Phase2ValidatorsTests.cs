using FluentValidation.TestHelper;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Applications.Validators;
using WMBankPortal.Application.Features.Profile.DTOs;
using WMBankPortal.Application.Features.Profile.Validators;
using Xunit;

namespace WMBankPortal.UnitTests;

public class Phase2ValidatorsTests
{
    private readonly UpdateProfileRequestValidator _profileValidator = new();
    private readonly SubmitApplicationRequestValidator _appValidator = new();

    [Fact]
    public void ProfileValidator_WithValidData_PassesValidation()
    {
        var request = new UpdateProfileRequest(
            FirstName: "Daksh",
            LastName: "Gupta",
            PhoneNumber: "+91 98765 43210",
            DateOfBirth: DateTime.UtcNow.AddYears(-25),
            AddressLine1: "123 Nariman Point",
            City: "Mumbai",
            PostalCode: "400021",
            Country: "India");

        var result = _profileValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProfileValidator_Under18YearsOld_FailsValidation()
    {
        var request = new UpdateProfileRequest(
            FirstName: "Daksh",
            LastName: "Gupta",
            PhoneNumber: "+91 98765 43210",
            DateOfBirth: DateTime.UtcNow.AddYears(-17), // 17 years old
            AddressLine1: "123 Nariman Point",
            City: "Mumbai",
            PostalCode: "400021",
            Country: "India");

        var result = _profileValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Customer must be at least 18 years old to hold an account.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-phone-abc")]
    [InlineData("123")]
    public void ProfileValidator_InvalidPhoneNumber_FailsValidation(string invalidPhone)
    {
        var request = new UpdateProfileRequest(
            FirstName: "Daksh",
            LastName: "Gupta",
            PhoneNumber: invalidPhone,
            DateOfBirth: DateTime.UtcNow.AddYears(-25),
            AddressLine1: "123 Nariman Point",
            City: "Mumbai",
            PostalCode: "400021",
            Country: "India");

        var result = _profileValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void SubmitApplicationValidator_WithValidJsonAndAmount_PassesValidation()
    {
        var request = new SubmitApplicationRequest(
            ProductId: Guid.NewGuid(),
            RequestedAmount: 50000m,
            FormValuesJson: "{\"annualIncome\": 1200000, \"employerName\": \"Acme Corp\"}");

        var result = _appValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void SubmitApplicationValidator_InvalidAmount_FailsValidation(decimal amount)
    {
        var request = new SubmitApplicationRequest(
            ProductId: Guid.NewGuid(),
            RequestedAmount: amount,
            FormValuesJson: "{\"field\": \"value\"}");

        var result = _appValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RequestedAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-json")]
    [InlineData("[1,2,3]")] // Must be JSON object
    public void SubmitApplicationValidator_InvalidFormJson_FailsValidation(string badJson)
    {
        var request = new SubmitApplicationRequest(
            ProductId: Guid.NewGuid(),
            RequestedAmount: 100000m,
            FormValuesJson: badJson);

        var result = _appValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FormValuesJson);
    }
}
