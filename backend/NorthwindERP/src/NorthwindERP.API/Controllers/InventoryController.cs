using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Controllers;

/// <summary>Inventario: stock, Kardex y almacenes (solo lectura)</summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory) => _inventory = inventory;

    /// <summary>
    /// Stock actual por almacén, leído de la vista vw_stock_status. Cada fila trae
    /// needsRestock (cantidad &lt;= mínimo) y kardexOk (el stock cuadra con el Kardex).
    /// </summary>
    /// <param name="warehouseId">Solo ese almacén.</param>
    /// <param name="productId">Solo ese producto.</param>
    /// <param name="lowStockOnly">true = solo lo que hay que reponer.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("stock")]
    public async Task<ActionResult<IEnumerable<StockDto>>> GetStock(
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] bool lowStockOnly,
        CancellationToken ct)
    {
        var stock = await _inventory.GetStockAsync(new StockFilter(warehouseId, productId, lowStockOnly), ct);
        return Ok(stock.Select(s => s.ToDto()));
    }
}
