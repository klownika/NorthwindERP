using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Transportistas</summary>
[ApiController]
[Route("api/shippers")]
[Authorize]
public class ShippersController : ControllerBase
{
    private readonly ILogisticsService _logisticsService;

    public ShippersController(ILogisticsService logisticsService)
    {
        _logisticsService = logisticsService;
    }

    /// <summary>
    /// Lista los transportistas. El id elegido es el shipperId que se manda
    /// a POST /api/orders/with-customer.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ShipperDto>>> GetAll(CancellationToken ct)
    {
        List<ShipperDto> shippers = await _logisticsService.GetShippersAsync(ct);
        return Ok(shippers);
    }
}
