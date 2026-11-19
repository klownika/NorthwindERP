using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;
using static NorthwindERP.Infrastructure.Data.DbHelpers;

namespace NorthwindERP.Infrastructure.Services;

/// <summary>
/// Reportes de ventas. Los dos primeros leen vistas ya armadas en la BD; los otros dos
/// son la consulta con JOIN + GROUP BY + COUNT/SUM/AVG que pide el enunciado.
/// El total de cada línea es Quantity * Products.Price, igual que en las vistas (regla R4).
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly NpgsqlDataSource _ds;

    public ReportService(NpgsqlDataSource ds) => _ds = ds;

    // COUNT y SUM de enteros salen como bigint en PostgreSQL; el ::int es para leerlos como int
    public Task<IReadOnlyList<CustomerSales>> GetSalesByCustomerAsync(CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT CustomerID, CustomerName, Country,
                   TotalOrders::int AS TotalOrders, TotalUnits::int AS TotalUnits, TotalSales
            FROM vw_sales_by_customer
            ORDER BY TotalSales DESC
            """,
            r => new CustomerSales(
                r.Get<int>("customerid"),
                r.Get<string>("customername"),
                r.GetStringOrNull("country"),
                r.Get<int>("totalorders"),
                r.Get<int>("totalunits"),
                r.Get<decimal>("totalsales")),
            ct);

    public Task<IReadOnlyList<CountrySales>> GetSalesByCountryAsync(CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT Country, TotalCustomers::int AS TotalCustomers, TotalOrders::int AS TotalOrders, TotalSales
            FROM vw_sales_by_country
            ORDER BY TotalSales DESC
            """,
            r => new CountrySales(
                r.GetStringOrNull("country"),
                r.Get<int>("totalcustomers"),
                r.Get<int>("totalorders"),
                r.Get<decimal>("totalsales")),
            ct);

    // COUNT(DISTINCT OrderID) porque el JOIN con OrderDetails repite el pedido una vez por línea
    public Task<IReadOnlyList<CustomerYearSales>> GetSalesByCustomerYearAsync(int? year, int? customerId, CancellationToken ct)
        => _ds.QueryAsync(
            """
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
            """,
            r => new CustomerYearSales(
                r.Get<int>("customerid"),
                r.Get<string>("customername"),
                r.Get<int>("salesyear"),
                r.Get<int>("totalorders"),
                r.Get<decimal>("totalsales"),
                r.Get<decimal>("avglinetotal")),
            ct,
            P("year", year, NpgsqlDbType.Integer),
            P("customer", customerId, NpgsqlDbType.Integer));

    // El ticket promedio se calcula a mano (total / pedidos): un AVG directo daría
    // el promedio por línea, no por pedido
    public Task<IReadOnlyList<EmployeeYearSales>> GetSalesByEmployeeYearAsync(int? year, CancellationToken ct)
        => _ds.QueryAsync(
            """
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
            """,
            r => new EmployeeYearSales(
                r.Get<int>("employeeid"),
                r.Get<string>("employeename"),
                r.Get<int>("salesyear"),
                r.Get<int>("totalorders"),
                r.Get<decimal>("totalsales"),
                r.Get<decimal>("avgordervalue")),
            ct,
            P("year", year, NpgsqlDbType.Integer));
}
