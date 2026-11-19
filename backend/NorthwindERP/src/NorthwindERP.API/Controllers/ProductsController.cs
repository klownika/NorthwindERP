using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;

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

    /// <summary>
    /// Registra un producto. En la misma transacción le crea su fila en InventoryStock
    /// con cantidad 0 en cada almacén, para que ya se pueda vender cuando entre stock.
    /// Si la categoría o el proveedor no existen responde 400.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = await _products.CreateAsync(request.ToModel(), ct);
        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product.ToDto());
    }

    /// <summary>
    /// Edita nombre, categoría, proveedor, unidad o precio. El stock no se toca aquí,
    /// ese solo cambia con los pedidos (regla R8).
    /// </summary>
    /// <remarks>
    /// El precio solo lo cambia un Admin: las vistas calculan los totales con el precio
    /// actual de Products (no se guarda el precio de cada venta), así que cambiarlo
    /// también cambia los totales de los pedidos viejos y de los reportes.
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        if (request.Price is not null && !User.IsInRole(Roles.Admin))
            return Problem(
                title: "Forbidden",
                detail: "Only an Admin can change the price.",
                statusCode: StatusCodes.Status403Forbidden);

        var product = await _products.UpdateAsync(id, request.ToModel(), ct);
        return Ok(product.ToDto());
    }

    /// <summary>Lista las categorías; son los filtros (pills) que salen arriba del catálogo.</summary>
    // La "/" del inicio hace que la ruta no herede el api/products del controller
    [HttpGet("/api/categories")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(CancellationToken ct)
    {
        var categories = await _inventory.GetCategoriesAsync(ct);
        return Ok(categories.Select(c => c.ToDto()));
    }
}
