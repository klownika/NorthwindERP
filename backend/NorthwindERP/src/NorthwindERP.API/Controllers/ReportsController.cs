using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Reportes de ventas</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>
    /// Vista 2 (por cliente): total vendido a cada cliente, leído de vw_sales_by_customer
    /// y ordenado de mayor a menor. Los clientes que nunca compraron no aparecen,
    /// porque la vista usa JOIN con Orders (regla R9).
    /// </summary>
    [HttpGet("sales-by-customer")]
    public async Task<ActionResult<IEnumerable<CustomerSalesDto>>> SalesByCustomer(CancellationToken ct)
    {
        var rows = await _reports.GetSalesByCustomerAsync(ct);
        return Ok(rows.Select(r => r.ToDto()));
    }
}
