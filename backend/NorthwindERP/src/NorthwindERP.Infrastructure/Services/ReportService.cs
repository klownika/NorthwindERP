using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// Reportes de ventas con SQL directo. Los dos primeros leen vistas de la BD; los otros dos
// son la consulta con JOIN + GROUP BY + COUNT/SUM/AVG que pide el curso.
// El total de cada línea es Quantity * Products.Price, igual que en las vistas (regla R4).
//
// Nota: COUNT y SUM de enteros salen como bigint en PostgreSQL; el "::int" los convierte
// a int para poder leerlos como int en C#.
public class ReportService : IReportService
{
    private readonly NpgsqlDataSource _dataSource;

    public ReportService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<CustomerSalesDto>> GetSalesByCustomerAsync(CancellationToken ct)
    {
        // 1. SQL: vista 2 por cliente, de mayor a menor venta
        string sql = """
            SELECT CustomerID, CustomerName, Country,
                   TotalOrders::int AS TotalOrders, TotalUnits::int AS TotalUnits, TotalSales
            FROM vw_sales_by_customer
            ORDER BY TotalSales DESC
            """;

        // 2-3. Ejecutar
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<CustomerSalesDto> result = new List<CustomerSalesDto>();
        while (await reader.ReadAsync(ct))
        {
            CustomerSalesDto row = new CustomerSalesDto();
            row.CustomerId = SqlHelper.ReadInt(reader, "customerid");
            row.CustomerName = SqlHelper.ReadString(reader, "customername");
            row.Country = SqlHelper.ReadNullableString(reader, "country");
            row.TotalOrders = SqlHelper.ReadInt(reader, "totalorders");
            row.TotalUnits = SqlHelper.ReadInt(reader, "totalunits");
            row.TotalSales = SqlHelper.ReadDecimal(reader, "totalsales");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<CountrySalesDto>> GetSalesByCountryAsync(CancellationToken ct)
    {
        // 1. SQL: vista 2 por país
        string sql = """
            SELECT Country, TotalCustomers::int AS TotalCustomers, TotalOrders::int AS TotalOrders, TotalSales
            FROM vw_sales_by_country
            ORDER BY TotalSales DESC
            """;

        // 2-3. Ejecutar
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<CountrySalesDto> result = new List<CountrySalesDto>();
        while (await reader.ReadAsync(ct))
        {
            CountrySalesDto row = new CountrySalesDto();
            row.Country = SqlHelper.ReadNullableString(reader, "country");
            row.TotalCustomers = SqlHelper.ReadInt(reader, "totalcustomers");
            row.TotalOrders = SqlHelper.ReadInt(reader, "totalorders");
            row.TotalSales = SqlHelper.ReadDecimal(reader, "totalsales");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<CustomerYearSalesDto>> GetSalesByCustomerYearAsync(int? year, int? customerId, CancellationToken ct)
    {
        // 1. SQL: COUNT(DISTINCT OrderID) porque el JOIN con OrderDetails
        //    repite el pedido una vez por cada producto
        string sql = """
            SELECT c.CustomerID, c.CustomerName,
                   EXTRACT(YEAR FROM o.OrderDate)::int   AS SalesYear,
                   COUNT(DISTINCT o.OrderID)::int        AS TotalOrders,
                   SUM(od.Quantity * p.Price)            AS TotalSales,
                   ROUND(AVG(od.Quantity * p.Price), 2)  AS AvgLineTotal
            FROM Customers c
            JOIN Orders o        ON o.CustomerID = c.CustomerID
            JOIN OrderDetails od ON od.OrderID   = o.OrderID
            JOIN Products p      ON p.ProductID  = od.ProductID
            WHERE (@year     IS NULL OR EXTRACT(YEAR FROM o.OrderDate) = @year)
              AND (@customer IS NULL OR c.CustomerID = @customer)
            GROUP BY c.CustomerID, c.CustomerName, EXTRACT(YEAR FROM o.OrderDate)
            ORDER BY SalesYear, TotalSales DESC
            """;

        // 2. Parámetros
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("year", year, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("customer", customerId, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<CustomerYearSalesDto> result = new List<CustomerYearSalesDto>();
        while (await reader.ReadAsync(ct))
        {
            CustomerYearSalesDto row = new CustomerYearSalesDto();
            row.CustomerId = SqlHelper.ReadInt(reader, "customerid");
            row.CustomerName = SqlHelper.ReadString(reader, "customername");
            row.SalesYear = SqlHelper.ReadInt(reader, "salesyear");
            row.TotalOrders = SqlHelper.ReadInt(reader, "totalorders");
            row.TotalSales = SqlHelper.ReadDecimal(reader, "totalsales");
            row.AvgLineTotal = SqlHelper.ReadDecimal(reader, "avglinetotal");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<EmployeeYearSalesDto>> GetSalesByEmployeeYearAsync(int? year, CancellationToken ct)
    {
        // 1. SQL: el ticket promedio se calcula a mano (total / pedidos);
        //    un AVG directo daría el promedio por línea, no por pedido
        string sql = """
            SELECT e.EmployeeID,
                   e.FirstName || ' ' || e.LastName      AS EmployeeName,
                   EXTRACT(YEAR FROM o.OrderDate)::int   AS SalesYear,
                   COUNT(DISTINCT o.OrderID)::int        AS TotalOrders,
                   SUM(od.Quantity * p.Price)            AS TotalSales,
                   ROUND(SUM(od.Quantity * p.Price) / COUNT(DISTINCT o.OrderID), 2) AS AvgOrderValue
            FROM Employees e
            JOIN Orders o        ON o.EmployeeID = e.EmployeeID
            JOIN OrderDetails od ON od.OrderID   = o.OrderID
            JOIN Products p      ON p.ProductID  = od.ProductID
            WHERE (@year IS NULL OR EXTRACT(YEAR FROM o.OrderDate) = @year)
            GROUP BY e.EmployeeID, e.FirstName, e.LastName, EXTRACT(YEAR FROM o.OrderDate)
            ORDER BY SalesYear, TotalSales DESC
            """;

        // 2. Parámetro
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("year", year, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<EmployeeYearSalesDto> result = new List<EmployeeYearSalesDto>();
        while (await reader.ReadAsync(ct))
        {
            EmployeeYearSalesDto row = new EmployeeYearSalesDto();
            row.EmployeeId = SqlHelper.ReadInt(reader, "employeeid");
            row.EmployeeName = SqlHelper.ReadString(reader, "employeename");
            row.SalesYear = SqlHelper.ReadInt(reader, "salesyear");
            row.TotalOrders = SqlHelper.ReadInt(reader, "totalorders");
            row.TotalSales = SqlHelper.ReadDecimal(reader, "totalsales");
            row.AvgOrderValue = SqlHelper.ReadDecimal(reader, "avgordervalue");

            result.Add(row);
        }

        return result;
    }
}
