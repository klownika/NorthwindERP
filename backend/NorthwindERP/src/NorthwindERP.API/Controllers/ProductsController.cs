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
    private readonly IProductService _products;

    public ProductsController(IInventoryService inventory, IProductService products)
    {
        _inventory = inventory;
        _products = products;
    }

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

    /// <summary>Un producto con su categoría y proveedor. 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
        => Ok((await _products.GetByIdAsync(id, ct)).ToDto());

    /// <summary>Lista las categorías; son los filtros (pills) que salen arriba del catálogo.</summary>
    // La "/" del inicio hace que la ruta no herede el api/products del controller
    [HttpGet("/api/categories")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(CancellationToken ct)
    {
        var categories = await _inventory.GetCategoriesAsync(ct);
        return Ok(categories.Select(c => c.ToDto()));
    }
}
