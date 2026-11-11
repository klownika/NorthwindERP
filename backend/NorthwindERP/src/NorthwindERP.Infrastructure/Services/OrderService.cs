using System.Text.Json;
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

    public async Task<CreateOrderResult> CreateWithCustomerAsync(CreateOrderCommand command, CancellationToken ct)
    {
        await using var conn = await _ds.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);   // si algo falla, al liberar se hace ROLLBACK

        int customerId;
        if (command.NewCustomer is { } c)
        {
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO Customers (CustomerName, ContactName, Address, City, PostalCode, Country)
                VALUES (@name, @contact, @address, @city, @postal, @country)
                RETURNING CustomerID
                """, conn, tx);
            insert.Parameters.Add(P("name", c.CustomerName, NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("contact", c.ContactName, NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("address", c.Address, NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("city", c.City, NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("postal", c.PostalCode, NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("country", c.Country, NpgsqlDbType.Varchar));
            customerId = Convert.ToInt32(await insert.ExecuteScalarAsync(ct));
        }
        else
        {
            customerId = command.CustomerId!.Value;   // el procedimiento valida que exista (NW404)
        }

        // El procedimiento lee el JSON con product_id / quantity
        var details = JsonSerializer.Serialize(
            command.Items.Select(i => new { product_id = i.ProductId, quantity = i.Quantity }));

        int orderId;
        await using (var call = new NpgsqlCommand(
            "CALL sp_register_order(@customer, @employee, @warehouse, @details, @shipper, NULL)", conn, tx))
        {
            call.Parameters.Add(P("customer", customerId, NpgsqlDbType.Integer));
            call.Parameters.Add(P("employee", command.EmployeeId, NpgsqlDbType.Integer));
            call.Parameters.Add(P("warehouse", command.WarehouseId, NpgsqlDbType.Integer));
            call.Parameters.Add(P("details", details, NpgsqlDbType.Jsonb));
            call.Parameters.Add(P("shipper", command.ShipperId, NpgsqlDbType.Integer));
            orderId = Convert.ToInt32(await call.ExecuteScalarAsync(ct));   // valor del INOUT p_order_id
        }

        decimal total;
        await using (var sum = new NpgsqlCommand(
            "SELECT COALESCE(SUM(LineTotal), 0) FROM vw_order_details_extended WHERE OrderID = @id", conn, tx))
        {
            sum.Parameters.Add(P("id", orderId, NpgsqlDbType.Integer));
            total = Convert.ToDecimal(await sum.ExecuteScalarAsync(ct));
        }

        await tx.CommitAsync(ct);
        return new CreateOrderResult(orderId, customerId, total);
    }

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

    public Task<IReadOnlyList<OrderSummary>> GetAllAsync(OrderFilter f, CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT OrderID, OrderDate, CustomerID, CustomerName, EmployeeName, ShipperName,
                   COUNT(*)::int  AS LineCount,
                   SUM(LineTotal) AS OrderTotal
            FROM vw_order_details_extended
            WHERE (@customer IS NULL OR CustomerID = @customer)
              AND (@employee IS NULL OR EmployeeID = @employee)
              AND (@from     IS NULL OR OrderDate >= @from)
              AND (@to       IS NULL OR OrderDate <  @to + INTERVAL '1 day')
            GROUP BY OrderID, OrderDate, CustomerID, CustomerName, EmployeeName, ShipperName
            ORDER BY OrderDate DESC, OrderID DESC
            LIMIT @take
            """,
            MapSummary, ct,
            P("customer", f.CustomerId, NpgsqlDbType.Integer),
            P("employee", f.EmployeeId, NpgsqlDbType.Integer),
            P("from", f.From, NpgsqlDbType.Timestamp),
            P("to", f.To, NpgsqlDbType.Timestamp),
            P("take", f.Take, NpgsqlDbType.Integer));

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
