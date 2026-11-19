using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Transportistas</summary>
[ApiController]
[Route("api/shippers")]
[Authorize]
public class ShippersController : ControllerBase
{
    private readonly ILogisticsService _logistics;

    public ShippersController(ILogisticsService logistics) => _logistics = logistics;

    /// <summary>
    /// Lista los transportistas. Sale en el combo del formulario de nuevo pedido;
    /// el id elegido es el shipperId que se manda a POST /api/orders/with-customer.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShipperDto>>> GetAll(CancellationToken ct)
    {
        var shippers = await _logistics.GetShippersAsync(ct);
        return Ok(shippers.Select(s => s.ToDto()));
    }
}
