namespace WMBankPortal.Application.Features.Products.DTOs;

public record BankingProductDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    decimal InterestRate,
    decimal MinAmount,
    decimal MaxAmount,
    string DynamicFormSchemaJson,
    bool IsActive);
