using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Empleados</summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly ILogisticsService _logisticsService;

    public EmployeesController(ILogisticsService logisticsService)
    {
        _logisticsService = logisticsService;
    }

    /// <summary>
    /// Lista los empleados ordenados por apellido. El frontend lo usa para el filtro
    /// de vendedor en pedidos y para mostrar quién está a cargo de cada almacén.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll(CancellationToken ct)
    {
        List<EmployeeDto> employees = await _logisticsService.GetEmployeesAsync(ct);
        return Ok(employees);
    }
}
