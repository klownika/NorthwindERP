using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

/// <summary>Módulo de pedidos. La lógica de stock vive en la base (sp_register_order / sp_delete_order).</summary>
public interface IOrderService
{
    /// <summary>Devuelve null si el pedido no existe.</summary>
    Task<OrderDetail?> GetByIdAsync(int orderId, CancellationToken ct);
}
