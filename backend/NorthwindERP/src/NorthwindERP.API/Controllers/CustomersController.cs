using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Clientes</summary>
[ApiController]
[Route("api/customers")]
[Authorize] // todas las rutas de este controlador piden token (si no, 401)
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Lista todos los clientes.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken ct)
    {
        List<CustomerDto> customers = await _customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    /// <summary>Obtiene un cliente por id (404 si no existe).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
    {
        CustomerDto customer = await _customerService.GetByIdAsync(id, ct);
        return Ok(customer);
    }

    /// <summary>
    /// Registra un cliente sin pedido.
    /// Responde 201 con el header Location apuntando a GET /api/customers/{id}.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] NewCustomerRequest request, CancellationToken ct)
    {
        // 1. El servicio valida y guarda
        CustomerDto created = await _customerService.CreateAsync(request, ct);

        // 2. 201 Created + Location: /api/customers/{id}
        return CreatedAtAction(nameof(GetById), new { id = created.CustomerId }, created);
    }

    /// <summary>
    /// Actualiza la dirección de un cliente. La tabla customers no tiene teléfono,
    /// por eso solo se editan dirección, ciudad, código postal y país.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> UpdateContact(int id, [FromBody] UpdateCustomerContactRequest request, CancellationToken ct)
    {
        CustomerDto updated = await _customerService.UpdateContactAsync(id, request, ct);
        return Ok(updated);
    }
}
