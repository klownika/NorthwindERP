using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Reportes de ventas</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Vista 2 (por cliente): total vendido a cada cliente, de mayor a menor.
    /// Los clientes que nunca compraron no aparecen (regla R9).
    /// </summary>
    [HttpGet("sales-by-customer")]
    public async Task<ActionResult<List<CustomerSalesDto>>> SalesByCustomer(CancellationToken ct)
    {
        List<CustomerSalesDto> rows = await _reportService.GetSalesByCustomerAsync(ct);
        return Ok(rows);
    }

    /// <summary>Vista 2 (por región): total vendido por país, con clientes y pedidos.</summary>
    [HttpGet("sales-by-country")]
    public async Task<ActionResult<List<CountrySalesDto>>> SalesByCountry(CancellationToken ct)
    {
        List<CountrySalesDto> rows = await _reportService.GetSalesByCountryAsync(ct);
        return Ok(rows);
    }

    /// <summary>
    /// Ventas por cliente con totales por año (JOIN + GROUP BY + COUNT/SUM/AVG).
    /// Sin filtros devuelve todos los años.
    /// </summary>
    /// <param name="year">Solo ese año (1990-2100).</param>
    /// <param name="customerId">Solo ese cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("sales-by-customer-year")]
    public async Task<ActionResult<List<CustomerYearSalesDto>>> SalesByCustomerYear(
        [FromQuery, Range(1990, 2100)] int? year,
        [FromQuery] int? customerId,
        CancellationToken ct)
    {
        List<CustomerYearSalesDto> rows = await _reportService.GetSalesByCustomerYearAsync(year, customerId, ct);
        return Ok(rows);
    }

    /// <summary>
    /// Ventas por empleado con totales por año: pedidos, total vendido y ticket promedio.
    /// </summary>
    /// <param name="year">Solo ese año (1990-2100).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("sales-by-employee-year")]
    public async Task<ActionResult<List<EmployeeYearSalesDto>>> SalesByEmployeeYear(
        [FromQuery, Range(1990, 2100)] int? year,
        CancellationToken ct)
    {
        List<EmployeeYearSalesDto> rows = await _reportService.GetSalesByEmployeeYearAsync(year, ct);
        return Ok(rows);
    }
}
