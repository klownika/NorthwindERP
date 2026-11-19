using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;
using static NorthwindERP.Infrastructure.Data.DbHelpers;

namespace NorthwindERP.Infrastructure.Services;

/// <summary>
/// Lecturas de inventario con SQL directo. Aquí no hay ningún UPDATE a InventoryStock:
/// el stock lo mueven solo los procedimientos de pedidos (regla R8).
/// </summary>
/// <remarks>
/// Los filtros opcionales usan el patrón "(@p IS NULL OR columna = @p)": si el parámetro
/// llega null la condición se anula. Por eso cada parámetro va con su tipo explícito (P).
/// </remarks>
public sealed class InventoryService : IInventoryService
{
    private readonly NpgsqlDataSource _ds;

    public InventoryService(NpgsqlDataSource ds) => _ds = ds;

    // vw_stock_status ya trae NeedsRestock (Quantity <= MinStockLevel) y compara el stock
    // con la suma del Kardex. KardexBalance sale como bigint del SUM, por eso el ::int
    public Task<IReadOnlyList<StockItem>> GetStockAsync(StockFilter f, CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT WarehouseID, WarehouseName, ProductID, ProductName, Quantity, MinStockLevel,
                   NeedsRestock, KardexBalance::int AS KardexBalance, KardexOk
            FROM vw_stock_status
            WHERE (@warehouse IS NULL OR WarehouseID = @warehouse)
              AND (@product   IS NULL OR ProductID   = @product)
              AND (NOT @lowOnly OR NeedsRestock)
            ORDER BY WarehouseID, ProductID
            """,
            r => new StockItem(
                r.Get<int>("warehouseid"),
                r.Get<string>("warehousename"),
                r.Get<int>("productid"),
                r.Get<string>("productname"),
                r.Get<int>("quantity"),
                r.Get<int>("minstocklevel"),
                r.Get<bool>("needsrestock"),
                r.Get<int>("kardexbalance"),
                r.Get<bool>("kardexok")),
            ct,
            P("warehouse", f.WarehouseId, NpgsqlDbType.Integer),
            P("product", f.ProductId, NpgsqlDbType.Integer),
            P("lowOnly", f.LowStockOnly, NpgsqlDbType.Boolean));

    // El "to" es inclusive: se suma un día y se compara con < para no perder
    // los movimientos de la tarde de ese día
    public Task<IReadOnlyList<KardexEntry>> GetKardexAsync(KardexFilter f, CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT t.TransactionID, t.TransactionDate, t.WarehouseID, w.WarehouseName,
                   t.ProductID, p.ProductName, t.TransactionType, t.Quantity,
                   t.EmployeeID, e.FirstName || ' ' || e.LastName AS EmployeeName,
                   t.OrderID, t.Notes
            FROM InventoryTransactions t
            JOIN Warehouses w ON w.WarehouseID = t.WarehouseID
            JOIN Products p   ON p.ProductID   = t.ProductID
            JOIN Employees e  ON e.EmployeeID  = t.EmployeeID
            WHERE (@product   IS NULL OR t.ProductID       = @product)
              AND (@warehouse IS NULL OR t.WarehouseID     = @warehouse)
              AND (@type      IS NULL OR t.TransactionType = @type)
              AND (@from      IS NULL OR t.TransactionDate >= @from)
              AND (@to        IS NULL OR t.TransactionDate <  @to + INTERVAL '1 day')
            ORDER BY t.TransactionDate DESC, t.TransactionID DESC
            LIMIT @take
            """,
            r => new KardexEntry(
                r.Get<int>("transactionid"),
                r.Get<DateTime>("transactiondate"),
                r.Get<int>("warehouseid"),
                r.Get<string>("warehousename"),
                r.Get<int>("productid"),
                r.Get<string>("productname"),
                r.Get<string>("transactiontype"),
                r.Get<int>("quantity"),
                r.Get<int>("employeeid"),
                r.Get<string>("employeename"),
                r.GetIntOrNull("orderid"),
                r.GetStringOrNull("notes")),
            ct,
            P("product", f.ProductId, NpgsqlDbType.Integer),
            P("warehouse", f.WarehouseId, NpgsqlDbType.Integer),
            P("type", f.Type, NpgsqlDbType.Varchar),
            P("from", f.From, NpgsqlDbType.Timestamp),
            P("to", f.To, NpgsqlDbType.Timestamp),
            P("take", f.Take, NpgsqlDbType.Integer));

    public Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT w.WarehouseID, w.WarehouseName, w.Location, w.ManagerEmployeeID,
                   e.FirstName || ' ' || e.LastName AS ManagerName
            FROM Warehouses w
            JOIN Employees e ON e.EmployeeID = w.ManagerEmployeeID
            ORDER BY w.WarehouseID
            """,
            r => new Warehouse(
                r.Get<int>("warehouseid"),
                r.Get<string>("warehousename"),
                r.Get<string>("location"),
                r.Get<int>("manageremployeeid"),
                r.Get<string>("managername")),
            ct);

    // LEFT JOIN porque en Northwind la categoría y el proveedor pueden venir vacíos
    public Task<IReadOnlyList<Product>> GetProductsAsync(string? search, int? categoryId, CancellationToken ct)
        => _ds.QueryAsync(
            """
            SELECT p.ProductID, p.ProductName, p.CategoryID, c.CategoryName,
                   p.SupplierID, s.SupplierName, p.Unit, p.Price
            FROM Products p
            LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
            LEFT JOIN Suppliers s  ON s.SupplierID = p.SupplierID
            WHERE (@search   IS NULL OR p.ProductName ILIKE '%' || @search || '%')
              AND (@category IS NULL OR p.CategoryID = @category)
            ORDER BY p.ProductName
            """,
            r => new Product(
                r.Get<int>("productid"),
                r.Get<string>("productname"),
                r.GetIntOrNull("categoryid"),
                r.GetStringOrNull("categoryname"),
                r.GetIntOrNull("supplierid"),
                r.GetStringOrNull("suppliername"),
                r.GetStringOrNull("unit"),
                r.Get<decimal>("price")),
            ct,
            P("search", string.IsNullOrWhiteSpace(search) ? null : search.Trim(), NpgsqlDbType.Text),
            P("category", categoryId, NpgsqlDbType.Integer));

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct)
        => _ds.QueryAsync(
            "SELECT CategoryID, CategoryName, Description FROM Categories ORDER BY CategoryName",
            r => new Category(
                r.Get<int>("categoryid"),
                r.Get<string>("categoryname"),
                r.GetStringOrNull("description")),
            ct);
}
