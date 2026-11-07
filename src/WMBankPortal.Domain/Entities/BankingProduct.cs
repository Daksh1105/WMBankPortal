using WMBankPortal.Domain.Common;

namespace WMBankPortal.Domain.Entities;

public class BankingProduct : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal InterestRate { get; private set; }
    public decimal MinAmount { get; private set; }
    public decimal MaxAmount { get; private set; }
    public string DynamicFormSchemaJson { get; private set; } = "[]";
    public bool IsActive { get; private set; } = true;

    private BankingProduct() { } // For EF Core

    public BankingProduct(
        string code,
        string name,
        string category,
        decimal interestRate,
        decimal minAmount,
        decimal maxAmount,
        string dynamicFormSchemaJson,
        bool isActive = true)
    {
        Code = code;
        Name = name;
        Category = category;
        InterestRate = interestRate;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        DynamicFormSchemaJson = dynamicFormSchemaJson;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
