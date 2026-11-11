using NorthwindERP.Core.Entities;

namespace NorthwindERP.Core.Models;

/// <summary>Línea del pedido. Sin precio: la base usa Products.Price.</summary>
public record OrderLineItem(int ProductId, int Quantity);

/// <summary>
/// Pedido a registrar. Viene informado exactamente uno de CustomerId / NewCustomer.
/// EmployeeId sale del token, nunca del body.
/// </summary>
public record CreateOrderCommand(
    int? CustomerId,
    Customer? NewCustomer,
    int EmployeeId,
    int WarehouseId,
    int? ShipperId,
    IReadOnlyList<OrderLineItem> Items);

public record CreateOrderResult(int OrderId, int CustomerId, decimal OrderTotal);

public record OrderFilter(int? CustomerId, int? EmployeeId, DateTime? From, DateTime? To, int Take);

/// <summary>Cabecera de un pedido con su total.</summary>
public record OrderSummary(
    int OrderId,
    DateTime OrderDate,
    int CustomerId,
    string CustomerName,
    string EmployeeName,
    string? ShipperName,
    int LineCount,
    decimal OrderTotal);

/// <summary>Una fila de vw_order_details_extended.</summary>
public record OrderLine(
    int ProductId,
    string ProductName,
    string? CategoryName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDetail(OrderSummary Header, IReadOnlyList<OrderLine> Lines);

/// <summary>Una fila de get_customer_order_history(customerId).</summary>
public record CustomerOrderHistoryLine(
    int OrderId,
    DateTime OrderDate,
    string? ShipperName,
    string EmployeeName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
