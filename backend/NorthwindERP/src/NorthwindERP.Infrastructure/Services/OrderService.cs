using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// Pedidos con SQL directo (Npgsql). Las reglas de stock viven en la base de datos
// (sp_register_order / sp_delete_order); aquí solo se arma la transacción y se leen las vistas.
public class OrderService : IOrderService
{
    private readonly NpgsqlDataSource _dataSource;

    public OrderService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<CreateOrderResponse> CreateWithCustomerAsync(CreateOrderWithCustomerRequest request, int employeeId, CancellationToken ct)
    {
        // 1. Abrir UNA conexión y UNA transacción para todo el pedido.
        //    Si algo falla antes del Commit, al salir del método se hace ROLLBACK solo:
        //    no se guarda ni el cliente nuevo ni el pedido.
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);

        // 2. Saber el id del cliente: insertarlo si es nuevo, o usar el que vino
        int customerId;
        if (request.NewCustomer != null)
        {
            customerId = await InsertCustomerAsync(request.NewCustomer, connection, transaction, ct);
        }
        else
        {
            // El procedimiento valida que el cliente exista (error NW404)
            customerId = request.CustomerId!.Value;
        }

        // 3. Convertir los productos a JSON, que es lo que recibe el procedimiento:
        //    [ { "product_id": 11, "quantity": 2 }, ... ]
        List<object> itemsForJson = new List<object>();
        foreach (OrderItemRequest item in request.Items)
        {
            itemsForJson.Add(new { product_id = item.ProductId, quantity = item.Quantity });
        }
        string detailsJson = JsonSerializer.Serialize(itemsForJson);

        // 4. Llamar al procedimiento: valida stock, lo descuenta y registra el Kardex.
        //    Devuelve el id del pedido nuevo en el parámetro INOUT p_order_id.
        int orderId;
        string callSql = "CALL sp_register_order(@customer, @employee, @warehouse, @details, @shipper, NULL)";
        await using (NpgsqlCommand call = new NpgsqlCommand(callSql, connection, transaction))
        {
            call.Parameters.Add(SqlHelper.CreateParameter("customer", customerId, NpgsqlDbType.Integer));
            call.Parameters.Add(SqlHelper.CreateParameter("employee", employeeId, NpgsqlDbType.Integer));
            call.Parameters.Add(SqlHelper.CreateParameter("warehouse", request.WarehouseId, NpgsqlDbType.Integer));
            call.Parameters.Add(SqlHelper.CreateParameter("details", detailsJson, NpgsqlDbType.Jsonb));
            call.Parameters.Add(SqlHelper.CreateParameter("shipper", request.ShipperId, NpgsqlDbType.Integer));

            object? callResult = await call.ExecuteScalarAsync(ct);
            orderId = Convert.ToInt32(callResult);
        }

        // 5. Calcular el total del pedido desde la vista (dentro de la misma transacción)
        decimal total;
        string totalSql = "SELECT COALESCE(SUM(LineTotal), 0) FROM vw_order_details_extended WHERE OrderID = @id";
        await using (NpgsqlCommand sum = new NpgsqlCommand(totalSql, connection, transaction))
        {
            sum.Parameters.Add(SqlHelper.CreateParameter("id", orderId, NpgsqlDbType.Integer));

            object? sumResult = await sum.ExecuteScalarAsync(ct);
            total = Convert.ToDecimal(sumResult);
        }

        // 6. Todo salió bien: confirmar los cambios
        await transaction.CommitAsync(ct);

        CreateOrderResponse response = new CreateOrderResponse();
        response.OrderId = orderId;
        response.CustomerId = customerId;
        response.OrderTotal = total;

        return response;
    }

    public async Task<List<OrderSummaryDto>> GetAllAsync(int? customerId, int? employeeId, DateTime? from, DateTime? to, int take, CancellationToken ct)
    {
        // 1. SQL: la vista tiene una fila por producto, por eso se agrupa por pedido
        string sql = """
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
            """;

        // 2. Parámetros
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("customer", customerId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("employee", employeeId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("from", from, NpgsqlDbType.Timestamp));
        command.Parameters.Add(SqlHelper.CreateParameter("to", to, NpgsqlDbType.Timestamp));
        command.Parameters.Add(SqlHelper.CreateParameter("take", take, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<OrderSummaryDto> result = new List<OrderSummaryDto>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(ReadOrderSummary(reader));
        }

        return result;
    }

    public async Task<OrderDetailDto?> GetByIdAsync(int orderId, CancellationToken ct)
    {
        // PASO A: la cabecera. Sale de Orders para que también aparezca un pedido sin detalles.
        string headerSql = """
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
            """;

        OrderSummaryDto? header = null;
        await using (NpgsqlCommand headerCommand = _dataSource.CreateCommand(headerSql))
        {
            headerCommand.Parameters.Add(SqlHelper.CreateParameter("id", orderId, NpgsqlDbType.Integer));

            await using NpgsqlDataReader headerReader = await headerCommand.ExecuteReaderAsync(ct);
            if (await headerReader.ReadAsync(ct))
            {
                header = ReadOrderSummary(headerReader);
            }
        }

        // Si no hay cabecera, el pedido no existe (el controlador responde 404)
        if (header == null)
        {
            return null;
        }

        // PASO B: las líneas (productos) del pedido
        string linesSql = """
            SELECT ProductID, ProductName, CategoryName, Quantity, UnitPrice, LineTotal
            FROM vw_order_details_extended
            WHERE OrderID = @id
            ORDER BY ProductName
            """;

        List<OrderLineDto> lines = new List<OrderLineDto>();
        await using (NpgsqlCommand linesCommand = _dataSource.CreateCommand(linesSql))
        {
            linesCommand.Parameters.Add(SqlHelper.CreateParameter("id", orderId, NpgsqlDbType.Integer));

            await using NpgsqlDataReader linesReader = await linesCommand.ExecuteReaderAsync(ct);
            while (await linesReader.ReadAsync(ct))
            {
                OrderLineDto line = new OrderLineDto();
                line.ProductId = SqlHelper.ReadInt(linesReader, "productid");
                line.ProductName = SqlHelper.ReadString(linesReader, "productname");
                line.CategoryName = SqlHelper.ReadNullableString(linesReader, "categoryname");
                line.Quantity = SqlHelper.ReadInt(linesReader, "quantity");
                line.UnitPrice = SqlHelper.ReadDecimal(linesReader, "unitprice");
                line.LineTotal = SqlHelper.ReadDecimal(linesReader, "linetotal");

                lines.Add(line);
            }
        }

        // PASO C: juntar cabecera + líneas
        OrderDetailDto detail = new OrderDetailDto();
        detail.Order = header;
        detail.Lines = lines;

        return detail;
    }

    public async Task<List<CustomerOrderHistoryDto>> GetCustomerHistoryAsync(int customerId, CancellationToken ct)
    {
        // 1. SQL: la función get_customer_order_history (procedimiento 2 del curso)
        string sql = "SELECT * FROM get_customer_order_history(@id)";

        // 2-3. Parámetro y ejecutar
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("id", customerId, NpgsqlDbType.Integer));
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas (lista vacía si el cliente no tiene pedidos)
        List<CustomerOrderHistoryDto> result = new List<CustomerOrderHistoryDto>();
        while (await reader.ReadAsync(ct))
        {
            CustomerOrderHistoryDto row = new CustomerOrderHistoryDto();
            row.OrderId = SqlHelper.ReadInt(reader, "orderid");
            row.OrderDate = SqlHelper.ReadDateTime(reader, "orderdate");
            row.ShipperName = SqlHelper.ReadNullableString(reader, "shippername");
            row.EmployeeName = SqlHelper.ReadNullableString(reader, "employeename") ?? "";
            row.ProductName = SqlHelper.ReadString(reader, "productname");
            row.Quantity = SqlHelper.ReadInt(reader, "quantity");
            row.UnitPrice = SqlHelper.ReadDecimal(reader, "unitprice");
            row.LineTotal = SqlHelper.ReadDecimal(reader, "linetotal");

            result.Add(row);
        }

        return result;
    }

    public async Task DeleteAsync(int orderId, int employeeId, CancellationToken ct)
    {
        // El procedimiento devuelve el stock, registra el INBOUND en el Kardex
        // y borra los detalles en cascada. Si el pedido no existe lanza NW404.
        string sql = "CALL sp_delete_order(@order, @employee)";

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("order", orderId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("employee", employeeId, NpgsqlDbType.Integer));

        await command.ExecuteNonQueryAsync(ct);
    }

    // INSERT del cliente nuevo dentro de la transacción del pedido. Devuelve su id.
    private static async Task<int> InsertCustomerAsync(NewCustomerRequest newCustomer, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        string sql = """
            INSERT INTO Customers (CustomerName, ContactName, Address, City, PostalCode, Country)
            VALUES (@name, @contact, @address, @city, @postal, @country)
            RETURNING CustomerID
            """;

        await using NpgsqlCommand insert = new NpgsqlCommand(sql, connection, transaction);
        insert.Parameters.Add(SqlHelper.CreateParameter("name", newCustomer.CustomerName.Trim(), NpgsqlDbType.Varchar));
        insert.Parameters.Add(SqlHelper.CreateParameter("contact", newCustomer.ContactName?.Trim(), NpgsqlDbType.Varchar));
        insert.Parameters.Add(SqlHelper.CreateParameter("address", newCustomer.Address?.Trim(), NpgsqlDbType.Varchar));
        insert.Parameters.Add(SqlHelper.CreateParameter("city", newCustomer.City?.Trim(), NpgsqlDbType.Varchar));
        insert.Parameters.Add(SqlHelper.CreateParameter("postal", newCustomer.PostalCode?.Trim(), NpgsqlDbType.Varchar));
        insert.Parameters.Add(SqlHelper.CreateParameter("country", newCustomer.Country?.Trim(), NpgsqlDbType.Varchar));

        // RETURNING CustomerID devuelve el id generado
        object? result = await insert.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    // Lee la fila actual como cabecera de pedido (la usan GetAllAsync y GetByIdAsync)
    private static OrderSummaryDto ReadOrderSummary(NpgsqlDataReader reader)
    {
        OrderSummaryDto order = new OrderSummaryDto();
        order.OrderId = SqlHelper.ReadInt(reader, "orderid");
        order.OrderDate = SqlHelper.ReadDateTime(reader, "orderdate");
        order.CustomerId = SqlHelper.ReadInt(reader, "customerid");
        order.CustomerName = SqlHelper.ReadNullableString(reader, "customername") ?? "";
        order.EmployeeName = SqlHelper.ReadNullableString(reader, "employeename") ?? "";
        order.ShipperName = SqlHelper.ReadNullableString(reader, "shippername");
        order.LineCount = SqlHelper.ReadInt(reader, "linecount");
        order.OrderTotal = SqlHelper.ReadDecimal(reader, "ordertotal");

        return order;
    }
}
