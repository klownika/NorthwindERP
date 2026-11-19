using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// Movimientos del Kardex (tabla InventoryTransactions), los más recientes primero.
    /// Cada venta hecha con sp_register_order deja un OUTBOUND "Sale - order N" y cada
    /// pedido eliminado con sp_delete_order deja el INBOUND que devuelve el stock.
    /// </summary>
    /// <param name="productId">Solo ese producto.</param>
    /// <param name="warehouseId">Solo ese almacén.</param>
    /// <param name="type">INBOUND u OUTBOUND.</param>
    /// <param name="from">Desde esta fecha (yyyy-MM-dd).</param>
    /// <param name="to">Hasta esta fecha, inclusive (yyyy-MM-dd).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="take">Cuántos movimientos devolver, de 1 a 1000 (500 si no se manda).</param>
    [HttpGet("kardex")]
    [ProducesResponseType(typeof(IEnumerable<KardexEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<KardexEntryDto>>> GetKardex(
        [FromQuery] int? productId,
        [FromQuery] int? warehouseId,
        [FromQuery] string? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct,
        [FromQuery, Range(1, 1000)] int take = 500)
    {
        // Se acepta "outbound" o "Outbound"; a la BD siempre llega en mayúsculas
        var normalizedType = type?.Trim().ToUpperInvariant();
        if (normalizedType is not null && !TransactionTypes.All.Contains(normalizedType))
        {
            ModelState.AddModelError(nameof(type), "Type must be INBOUND or OUTBOUND.");
            return ValidationProblem(ModelState);
        }

        var filter = new KardexFilter(productId, warehouseId, normalizedType, from, to, take);
        var entries = await _inventory.GetKardexAsync(filter, ct);
        return Ok(entries.Select(e => e.ToDto()));
    }

    /// <summary>
    /// Lista los almacenes con el nombre de su responsable (join de Warehouses con
    /// Employees por ManagerEmployeeID). Es el warehouseId que pide el formulario de pedido.
    /// </summary>
    [HttpGet("warehouses")]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        var warehouses = await _inventory.GetWarehousesAsync(ct);
        return Ok(warehouses.Select(w => w.ToDto()));
    }
}
