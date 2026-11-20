using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Proveedores</summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ILogisticsService _logisticsService;

    public SuppliersController(ILogisticsService logisticsService)
    {
        _logisticsService = logisticsService;
    }

    /// <summary>Lista los proveedores ordenados por nombre.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll(CancellationToken ct)
    {
        List<SupplierDto> suppliers = await _logisticsService.GetSuppliersAsync(ct);
        return Ok(suppliers);
    }
}
