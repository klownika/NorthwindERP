using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;
using static NorthwindERP.Infrastructure.Data.DbHelpers;

namespace NorthwindERP.Infrastructure.Services;

/// <summary>
/// Módulo de pedidos. La lógica de stock vive en la base (sp_register_order / sp_delete_order);
/// aquí solo se orquesta la transacción y se leen las vistas.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly NpgsqlDataSource _ds;

    public OrderService(NpgsqlDataSource ds) => _ds = ds;

    public async Task<OrderDetail?> GetByIdAsync(int orderId, CancellationToken ct)
    {
        // La cabecera sale de Orders para que también aparezca un pedido sin detalles
        var header = (await _ds.QueryAsync(
            """
            SELECT o.OrderID, o.OrderDate, o.CustomerID, c.CustomerName,
                   e.FirstName || ' ' || e.LastName AS EmployeeName,
                   sh.ShipperName,
                   (SELECT COUNT(*)::int FROM OrderDetails od WHERE od.OrderID = o.OrderID) AS LineCount,
                   COALESCE((SELECT SUM(v.LineTotal) FROM vw_order_details_extended v
                             WHERE v.OrderID = o.OrderID), 0) AS OrderTotal
            FROM Orders o
            LEFT JOIN Customers c ON c.CustomerID = o.CustomerID
            LEFT JOIN Employees e ON e.EmployeeID = o.EmployeeID
            LEFT JOIN Shippers sh ON sh.ShipperID = o.ShipperID
            WHERE o.OrderID = @id
            """,
            MapSummary, ct, P("id", orderId, NpgsqlDbType.Integer))).FirstOrDefault();

        if (header is null) return null;

        var lines = await _ds.QueryAsync(
            """
            SELECT ProductID, ProductName, CategoryName, Quantity, UnitPrice, LineTotal
            FROM vw_order_details_extended
            WHERE OrderID = @id
            ORDER BY ProductName
            """,
            r => new OrderLine(
                r.Get<int>("productid"),
                r.Get<string>("productname"),
                r.GetStringOrNull("categoryname"),
                r.Get<int>("quantity"),
                r.Get<decimal>("unitprice"),
                r.Get<decimal>("linetotal")),
            ct, P("id", orderId, NpgsqlDbType.Integer));

        return new OrderDetail(header, lines);
    }

    private static OrderSummary MapSummary(NpgsqlDataReader r) => new(
        r.Get<int>("orderid"),
        r.Get<DateTime>("orderdate"),
        r.Get<int>("customerid"),
        r.GetStringOrNull("customername") ?? "",
        r.GetStringOrNull("employeename") ?? "",
        r.GetStringOrNull("shippername"),
        r.Get<int>("linecount"),
        r.Get<decimal>("ordertotal"));
}
