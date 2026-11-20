using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Inventario: stock, Kardex y almacenes (solo lectura)</summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Stock actual por almacén (vista vw_stock_status). Cada fila trae
    /// needsRestock (hay que reponer) y kardexOk (el stock cuadra con el Kardex).
    /// </summary>
    /// <param name="warehouseId">Solo ese almacén.</param>
    /// <param name="productId">Solo ese producto.</param>
    /// <param name="lowStockOnly">true = solo lo que hay que reponer.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("stock")]
    public async Task<ActionResult<List<StockDto>>> GetStock(
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] bool lowStockOnly,
        CancellationToken ct)
    {
        List<StockDto> stock = await _inventoryService.GetStockAsync(warehouseId, productId, lowStockOnly, ct);
        return Ok(stock);
    }

    /// <summary>
    /// Movimientos del Kardex, los más recientes primero. Cada venta deja un OUTBOUND
    /// "Sale - order N" y cada pedido eliminado deja el INBOUND que devuelve el stock.
    /// </summary>
    /// <param name="productId">Solo ese producto.</param>
    /// <param name="warehouseId">Solo ese almacén.</param>
    /// <param name="type">INBOUND u OUTBOUND.</param>
    /// <param name="from">Desde esta fecha (yyyy-MM-dd).</param>
    /// <param name="to">Hasta esta fecha, inclusive (yyyy-MM-dd).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="take">Cuántos movimientos devolver, de 1 a 1000 (500 si no se manda).</param>
    [HttpGet("kardex")]
    public async Task<ActionResult<List<KardexEntryDto>>> GetKardex(
        [FromQuery] int? productId,
        [FromQuery] int? warehouseId,
        [FromQuery] string? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct,
        [FromQuery, Range(1, 1000)] int take = 500)
    {
        // 1. Aceptar "outbound" u "Outbound": a la base de datos siempre llega en mayúsculas
        string? normalizedType = null;
        if (type != null)
        {
            normalizedType = type.Trim().ToUpperInvariant();
        }

        // 2. Si mandaron un tipo, tiene que ser INBOUND u OUTBOUND -> si no, 400
        if (normalizedType != null
            && normalizedType != TransactionTypes.Inbound
            && normalizedType != TransactionTypes.Outbound)
        {
            ModelState.AddModelError(nameof(type), "Type must be INBOUND or OUTBOUND.");
            return ValidationProblem(ModelState);
        }

        // 3. Consultar
        List<KardexEntryDto> entries = await _inventoryService.GetKardexAsync(productId, warehouseId, normalizedType, from, to, take, ct);
        return Ok(entries);
    }

    /// <summary>
    /// Lista los almacenes con el nombre de su responsable.
    /// Es el warehouseId que pide el formulario de pedido.
    /// </summary>
    [HttpGet("warehouses")]
    public async Task<ActionResult<List<WarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        List<WarehouseDto> warehouses = await _inventoryService.GetWarehousesAsync(ct);
        return Ok(warehouses);
    }
}
