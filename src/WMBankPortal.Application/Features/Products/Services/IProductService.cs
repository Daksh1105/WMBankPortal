using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Features.Products.Services;

public interface IProductService
{
    Task<Result<IReadOnlyList<BankingProductDto>>> GetActiveProductsAsync(CancellationToken ct = default);
    Task<Result<BankingProductDto>> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
}
