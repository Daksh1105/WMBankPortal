using System.Text.Json;
using FluentValidation;
using WMBankPortal.Application.Features.Applications.DTOs;

namespace WMBankPortal.Application.Features.Applications.Validators;

public class SubmitApplicationRequestValidator : AbstractValidator<SubmitApplicationRequest>
{
    public SubmitApplicationRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("A banking product must be selected.");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0).WithMessage("Requested amount must be greater than zero.");

        RuleFor(x => x.FormValuesJson)
            .NotEmpty().WithMessage("Application form values are required.")
            .Must(BeValidJson).WithMessage("Form values must be a valid JSON payload.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
