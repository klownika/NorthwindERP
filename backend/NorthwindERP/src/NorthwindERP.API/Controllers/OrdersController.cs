using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Extensions;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Controllers;

/// <summary>Pedidos</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>
    /// Registra un pedido completo (cliente nuevo o existente, más sus productos) en una sola transacción.
    /// Llama a sp_register_order: valida el stock del almacén, lo descuenta y registra el OUTBOUND en el Kardex.
    /// Si falta stock de un producto no se guarda nada, ni siquiera el cliente nuevo.
    /// </summary>
    [HttpPost("with-customer")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateOrderResponse>> CreateWithCustomer(
        [FromBody] CreateOrderWithCustomerRequest request, CancellationToken ct)
    {
        var command = new CreateOrderCommand(
            CustomerId: request.CustomerId,
            NewCustomer: request.NewCustomer?.ToEntity(),
            EmployeeId: User.GetEmployeeId(),
            WarehouseId: request.WarehouseId,
            ShipperId: request.ShipperId,
            Items: request.Items.Select(i => new OrderLineItem(i.ProductId, i.Quantity)).ToList());

        var result = await _orders.CreateWithCustomerAsync(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result.ToDto());
    }

    /// <summary>Pedidos con su total, los más recientes primero.</summary>
    /// <param name="customerId">Filtra por cliente.</param>
    /// <param name="employeeId">Filtra por empleado.</param>
    /// <param name="from">Fecha inicial (yyyy-MM-dd).</param>
    /// <param name="to">Fecha final (yyyy-MM-dd, inclusive).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="take">Máximo de pedidos (1-1000, por defecto 200).</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct,
        [FromQuery, Range(1, 1000)] int take = 200)
    {
        var orders = await _orders.GetAllAsync(new OrderFilter(customerId, employeeId, from, to, take), ct);
        return Ok(orders.Select(o => o.ToDto()));
    }

    /// <summary>Un pedido con sus productos y el total por línea (vista vw_order_details_extended).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order.ToDto());
    }
}
