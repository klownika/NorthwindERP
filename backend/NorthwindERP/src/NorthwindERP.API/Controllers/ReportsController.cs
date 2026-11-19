using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// Vista 2 (por región): total vendido por país, con cuántos clientes compraron y
    /// cuántos pedidos hubo. Sale de vw_sales_by_country.
    /// </summary>
    [HttpGet("sales-by-country")]
    public async Task<ActionResult<IEnumerable<CountrySalesDto>>> SalesByCountry(CancellationToken ct)
    {
        var rows = await _reports.GetSalesByCountryAsync(ct);
        return Ok(rows.Select(r => r.ToDto()));
    }

    /// <summary>
    /// Ventas por cliente con totales por año: une Customers, Orders, OrderDetails y
    /// Products y agrupa por cliente y año (COUNT de pedidos, SUM del total y AVG por línea).
    /// Sin filtros devuelve todos los años.
    /// </summary>
    /// <param name="year">Solo ese año (1990-2100).</param>
    /// <param name="customerId">Solo ese cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("sales-by-customer-year")]
    [ProducesResponseType(typeof(IEnumerable<CustomerYearSalesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<CustomerYearSalesDto>>> SalesByCustomerYear(
        [FromQuery, Range(1990, 2100)] int? year, [FromQuery] int? customerId, CancellationToken ct)
    {
        var rows = await _reports.GetSalesByCustomerYearAsync(year, customerId, ct);
        return Ok(rows.Select(r => r.ToDto()));
    }
}
