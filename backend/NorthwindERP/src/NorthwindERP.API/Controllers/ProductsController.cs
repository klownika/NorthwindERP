using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Catálogo de productos</summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public ProductsController(IInventoryService inventory) => _inventory = inventory;

    /// <summary>
    /// Lista los productos con su categoría, proveedor y precio. Products.Price es el
    /// precio de venta que usan las vistas para calcular los totales (regla R4).
    /// </summary>
    /// <param name="search">Parte del nombre, sin importar mayúsculas (opcional).</param>
    /// <param name="categoryId">Solo productos de esa categoría (opcional).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] string? search, [FromQuery] int? categoryId, CancellationToken ct)
    {
        var products = await _inventory.GetProductsAsync(search, categoryId, ct);
        return Ok(products.Select(p => p.ToDto()));
    }
}
