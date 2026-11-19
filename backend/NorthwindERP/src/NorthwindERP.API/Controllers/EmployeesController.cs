using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Empleados</summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly ILogisticsService _logistics;

    public EmployeesController(ILogisticsService logistics) => _logistics = logistics;

    /// <summary>
    /// Lista los empleados ordenados por apellido. El frontend lo usa para el filtro
    /// de vendedor en pedidos y para mostrar quién está a cargo de cada almacén.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll(CancellationToken ct)
    {
        var employees = await _logistics.GetEmployeesAsync(ct);
        return Ok(employees.Select(e => e.ToDto()));
    }
}
