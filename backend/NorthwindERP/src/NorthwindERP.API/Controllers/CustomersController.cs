using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Controllers;

/// <summary>Clientes</summary>
[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    /// <summary>Lista todos los clientes.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(CancellationToken ct)
    {
        var customers = await _customers.GetAllAsync(ct);
        return Ok(customers.Select(c => c.ToDto()));
    }

    /// <summary>Obtiene un cliente por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
        => Ok((await _customers.GetByIdAsync(id, ct)).ToDto());

    /// <summary>
    /// Registra un cliente sin pedido (INSERT en customers con EF Core). Antes la única
    /// forma de crear clientes era POST /api/orders/with-customer junto con un pedido.
    /// Responde 201 con el header Location apuntando a GET /api/customers/{id}.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] NewCustomerRequest request, CancellationToken ct)
    {
        var customer = await _customers.CreateAsync(request.ToEntity(), ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer.ToDto());
    }

    /// <summary>
    /// Actualiza un cliente: nombre, contacto, dirección, ciudad, código postal y país.
    /// Todos son opcionales y solo cambia lo que se manda. La tabla customers no tiene
    /// teléfono, así que el requisito de "actualizar teléfono o dirección" se cubre con la dirección.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> UpdateContact(
        int id, [FromBody] UpdateCustomerContactRequest request, CancellationToken ct)
    {
        var update = new CustomerContactUpdate(
            request.CustomerName, request.ContactName,
            request.Address, request.City, request.PostalCode, request.Country);
        var customer = await _customers.UpdateContactAsync(id, update, ct);
        return Ok(customer.ToDto());
    }
}
