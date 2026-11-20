using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.Helpers;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

/// <summary>Pedidos</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Registra un pedido completo (cliente nuevo o existente, más sus productos) en una sola transacción.
    /// Llama a sp_register_order: valida el stock, lo descuenta y registra el OUTBOUND en el Kardex.
    /// Si falta stock (409) no se guarda nada, ni siquiera el cliente nuevo.
    /// </summary>
    [HttpPost("with-customer")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateOrderResponse>> CreateWithCustomer([FromBody] CreateOrderWithCustomerRequest request, CancellationToken ct)
    {
        // 1. El empleado que registra la venta sale del TOKEN, nunca del body
        int employeeId = UserClaims.GetEmployeeId(User);

        // 2. El servicio hace todo en una transacción
        CreateOrderResponse created = await _orderService.CreateWithCustomerAsync(request, employeeId, ct);

        // 3. 201 Created + Location: /api/orders/{id}
        return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, created);
    }

    /// <summary>Pedidos con su total, los más recientes primero.</summary>
    /// <param name="customerId">Filtra por cliente.</param>
    /// <param name="employeeId">Filtra por empleado.</param>
    /// <param name="from">Fecha inicial (yyyy-MM-dd).</param>
    /// <param name="to">Fecha final (yyyy-MM-dd, inclusive).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="take">Máximo de pedidos (1-1000, por defecto 200).</param>
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct,
        [FromQuery, Range(1, 1000)] int take = 200)
    {
        List<OrderSummaryDto> orders = await _orderService.GetAllAsync(customerId, employeeId, from, to, take, ct);
        return Ok(orders);
    }

    /// <summary>Un pedido con sus productos y el total por línea (vista vw_order_details_extended).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id, CancellationToken ct)
    {
        OrderDetailDto? order = await _orderService.GetByIdAsync(id, ct);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    /// <summary>Historial de pedidos de un cliente (función get_customer_order_history). Lista vacía si no tiene pedidos.</summary>
    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<List<CustomerOrderHistoryDto>>> GetCustomerHistory(int customerId, CancellationToken ct)
    {
        List<CustomerOrderHistoryDto> history = await _orderService.GetCustomerHistoryAsync(customerId, ct);
        return Ok(history);
    }

    /// <summary>
    /// Elimina un pedido (solo Admin). Llama a sp_delete_order: devuelve el stock al almacén,
    /// registra un INBOUND en el Kardex y borra los detalles por cascada.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)] // un usuario que no es Admin recibe 403
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        int employeeId = UserClaims.GetEmployeeId(User);

        await _orderService.DeleteAsync(id, employeeId, ct);

        return NoContent();
    }
}
