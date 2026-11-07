using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Application.Features.Products.Services;

namespace WMBankPortal.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankingProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetActiveProducts(CancellationToken ct)
    {
        var result = await _productService.GetActiveProductsAsync(ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BankingProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        var result = await _productService.GetProductByIdAsync(id, ct);
        return HandleResult(result);
    }
}
