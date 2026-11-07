using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;

namespace WMBankPortal.Application.Features.Products.Services;

public class ProductService : IProductService
{
    private readonly IAppDbContext _context;

    public ProductService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<BankingProductDto>>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        var rawList = await _context.BankingProducts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        IReadOnlyList<BankingProductDto> products = rawList.Select(MapToDto).ToList();
        return Result<IReadOnlyList<BankingProductDto>>.Success(products);
    }

    public async Task<Result<BankingProductDto>> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _context.BankingProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return Result<BankingProductDto>.Failure(
                Error.NotFound("Product.NotFound", $"Banking product with ID '{productId}' was not found."));
        }

        return Result<BankingProductDto>.Success(MapToDto(product));
    }

    private static BankingProductDto MapToDto(BankingProduct p) =>
        new(
            p.Id,
            p.Code,
            p.Name,
            p.Category,
            p.InterestRate,
            p.MinAmount,
            p.MaxAmount,
            p.DynamicFormSchemaJson,
            p.IsActive);
}
