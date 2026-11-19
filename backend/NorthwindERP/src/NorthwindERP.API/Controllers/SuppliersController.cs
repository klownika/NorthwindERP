using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Proveedores</summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ILogisticsService _logistics;

    public SuppliersController(ILogisticsService logistics) => _logistics = logistics;

    /// <summary>
    /// Lista los proveedores ordenados por nombre. Lo usa la pantalla de proveedores
    /// de inventario y el combo de proveedor al crear o editar un producto.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll(CancellationToken ct)
    {
        var suppliers = await _logistics.GetSuppliersAsync(ct);
        return Ok(suppliers.Select(s => s.ToDto()));
    }
}
