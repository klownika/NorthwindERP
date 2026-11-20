using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Catálogo de productos</summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public ProductsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Lista los productos con su categoría, proveedor y precio.
    /// Products.Price es el precio de venta que usan las vistas para los totales (regla R4).
    /// </summary>
    /// <param name="search">Parte del nombre, sin importar mayúsculas (opcional).</param>
    /// <param name="categoryId">Solo productos de esa categoría (opcional).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts([FromQuery] string? search, [FromQuery] int? categoryId, CancellationToken ct)
    {
        List<ProductDto> products = await _inventoryService.GetProductsAsync(search, categoryId, ct);
        return Ok(products);
    }

    /// <summary>Un producto con su categoría y proveedor. 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        ProductDto product = await _inventoryService.GetProductByIdAsync(id, ct);
        return Ok(product);
    }

    /// <summary>Lista las categorías (los filtros que salen arriba del catálogo).</summary>
    // La "/" del inicio hace que la ruta NO herede el "api/products" del controlador
    [HttpGet("/api/categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken ct)
    {
        List<CategoryDto> categories = await _inventoryService.GetCategoriesAsync(ct);
        return Ok(categories);
    }
}
