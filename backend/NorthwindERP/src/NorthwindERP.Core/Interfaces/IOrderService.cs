using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

// Pedidos. La lógica de stock vive en la base de datos (sp_register_order / sp_delete_order).
public interface IOrderService
{
    // Crea el cliente (si es nuevo) y llama a sp_register_order, todo en una transacción
    Task<CreateOrderResponse> CreateWithCustomerAsync(CreateOrderWithCustomerRequest request, int employeeId, CancellationToken ct);

    Task<List<OrderSummaryDto>> GetAllAsync(int? customerId, int? employeeId, DateTime? from, DateTime? to, int take, CancellationToken ct);

    // Devuelve null si el pedido no existe
    Task<OrderDetailDto?> GetByIdAsync(int orderId, CancellationToken ct);

    Task<List<CustomerOrderHistoryDto>> GetCustomerHistoryAsync(int customerId, CancellationToken ct);

    // Llama a sp_delete_order: devuelve el stock y borra en cascada
    Task DeleteAsync(int orderId, int employeeId, CancellationToken ct);
}
