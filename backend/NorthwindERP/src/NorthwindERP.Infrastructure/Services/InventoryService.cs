using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// Lecturas de inventario y catálogo con SQL directo (Npgsql).
// Aquí no hay ningún UPDATE al stock: solo lo mueven los procedimientos de pedidos (regla R8).
//
// Cómo funcionan los filtros opcionales:
//   WHERE (@warehouse IS NULL OR WarehouseID = @warehouse)
// Si @warehouse llega null, la condición siempre es verdadera y no filtra nada.
//
// Todos los métodos siguen los mismos 4 pasos:
//   1. Escribir el SQL   2. Agregar parámetros   3. Ejecutar   4. Leer fila por fila
public class InventoryService : IInventoryService
{
    private readonly NpgsqlDataSource _dataSource;

    public InventoryService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<StockDto>> GetStockAsync(int? warehouseId, int? productId, bool lowStockOnly, CancellationToken ct)
    {
        // 1. SQL: la vista vw_stock_status ya trae NeedsRestock y KardexOk calculados.
        //    KardexBalance sale como bigint del SUM, por eso el ::int
        string sql = """
            SELECT WarehouseID, WarehouseName, ProductID, ProductName, Quantity, MinStockLevel,
                   NeedsRestock, KardexBalance::int AS KardexBalance, KardexOk
            FROM vw_stock_status
            WHERE (@warehouse IS NULL OR WarehouseID = @warehouse)
              AND (@product   IS NULL OR ProductID   = @product)
              AND (NOT @lowOnly OR NeedsRestock)
            ORDER BY WarehouseID, ProductID
            """;

        // 2. Parámetros
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("warehouse", warehouseId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("product", productId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("lowOnly", lowStockOnly, NpgsqlDbType.Boolean));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer cada fila y convertirla en un DTO
        List<StockDto> result = new List<StockDto>();
        while (await reader.ReadAsync(ct))
        {
            StockDto row = new StockDto();
            row.WarehouseId = SqlHelper.ReadInt(reader, "warehouseid");
            row.WarehouseName = SqlHelper.ReadString(reader, "warehousename");
            row.ProductId = SqlHelper.ReadInt(reader, "productid");
            row.ProductName = SqlHelper.ReadString(reader, "productname");
            row.Quantity = SqlHelper.ReadInt(reader, "quantity");
            row.MinStockLevel = SqlHelper.ReadInt(reader, "minstocklevel");
            row.NeedsRestock = SqlHelper.ReadBool(reader, "needsrestock");
            row.KardexOk = SqlHelper.ReadBool(reader, "kardexok");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<KardexEntryDto>> GetKardexAsync(int? productId, int? warehouseId, string? type, DateTime? from, DateTime? to, int take, CancellationToken ct)
    {
        // 1. SQL: movimientos con el nombre del almacén, producto y empleado.
        //    El "to" es inclusive: se suma un día y se compara con < para no perder
        //    los movimientos de la tarde de ese día.
        string sql = """
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
            """;

        // 2. Parámetros
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("product", productId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("warehouse", warehouseId, NpgsqlDbType.Integer));
        command.Parameters.Add(SqlHelper.CreateParameter("type", type, NpgsqlDbType.Varchar));
        command.Parameters.Add(SqlHelper.CreateParameter("from", from, NpgsqlDbType.Timestamp));
        command.Parameters.Add(SqlHelper.CreateParameter("to", to, NpgsqlDbType.Timestamp));
        command.Parameters.Add(SqlHelper.CreateParameter("take", take, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<KardexEntryDto> result = new List<KardexEntryDto>();
        while (await reader.ReadAsync(ct))
        {
            KardexEntryDto row = new KardexEntryDto();
            row.TransactionId = SqlHelper.ReadInt(reader, "transactionid");
            row.TransactionDate = SqlHelper.ReadDateTime(reader, "transactiondate");
            row.WarehouseId = SqlHelper.ReadInt(reader, "warehouseid");
            row.WarehouseName = SqlHelper.ReadString(reader, "warehousename");
            row.ProductId = SqlHelper.ReadInt(reader, "productid");
            row.ProductName = SqlHelper.ReadString(reader, "productname");
            row.TransactionType = SqlHelper.ReadString(reader, "transactiontype");
            row.Quantity = SqlHelper.ReadInt(reader, "quantity");
            row.EmployeeId = SqlHelper.ReadInt(reader, "employeeid");
            row.EmployeeName = SqlHelper.ReadString(reader, "employeename");
            row.OrderId = SqlHelper.ReadNullableInt(reader, "orderid");
            row.Notes = SqlHelper.ReadNullableString(reader, "notes");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken ct)
    {
        // 1. SQL: cada almacén con el nombre de su responsable
        string sql = """
            SELECT w.WarehouseID, w.WarehouseName, w.Location, w.ManagerEmployeeID,
                   e.FirstName || ' ' || e.LastName AS ManagerName
            FROM Warehouses w
            JOIN Employees e ON e.EmployeeID = w.ManagerEmployeeID
            ORDER BY w.WarehouseID
            """;

        // 2-3. Sin parámetros: ejecutar directo
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<WarehouseDto> result = new List<WarehouseDto>();
        while (await reader.ReadAsync(ct))
        {
            WarehouseDto row = new WarehouseDto();
            row.WarehouseId = SqlHelper.ReadInt(reader, "warehouseid");
            row.WarehouseName = SqlHelper.ReadString(reader, "warehousename");
            row.Location = SqlHelper.ReadString(reader, "location");
            row.ManagerEmployeeId = SqlHelper.ReadInt(reader, "manageremployeeid");
            row.ManagerName = SqlHelper.ReadString(reader, "managername");

            result.Add(row);
        }

        return result;
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? search, int? categoryId, CancellationToken ct)
    {
        // 1. SQL: LEFT JOIN porque en Northwind la categoría y el proveedor pueden venir vacíos.
        //    ILIKE = LIKE sin importar mayúsculas.
        string sql = """
            SELECT p.ProductID, p.ProductName, p.CategoryID, c.CategoryName,
                   p.SupplierID, s.SupplierName, p.Unit, p.Price
            FROM Products p
            LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
            LEFT JOIN Suppliers s  ON s.SupplierID = p.SupplierID
            WHERE (@search   IS NULL OR p.ProductName ILIKE '%' || @search || '%')
              AND (@category IS NULL OR p.CategoryID = @category)
            ORDER BY p.ProductName
            """;

        // Si la búsqueda llega vacía o con puros espacios, no se filtra por nombre
        string? cleanSearch = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            cleanSearch = search.Trim();
        }

        // 2. Parámetros
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("search", cleanSearch, NpgsqlDbType.Text));
        command.Parameters.Add(SqlHelper.CreateParameter("category", categoryId, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Leer filas
        List<ProductDto> result = new List<ProductDto>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(ReadProduct(reader));
        }

        return result;
    }

    public async Task<ProductDto> GetProductByIdAsync(int productId, CancellationToken ct)
    {
        // 1. SQL: el mismo SELECT del catálogo, pero de un solo producto
        string sql = """
            SELECT p.ProductID, p.ProductName, p.CategoryID, c.CategoryName,
                   p.SupplierID, s.SupplierName, p.Unit, p.Price
            FROM Products p
            LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
            LEFT JOIN Suppliers s  ON s.SupplierID = p.SupplierID
            WHERE p.ProductID = @id
            """;

        // 2. Parámetro
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(SqlHelper.CreateParameter("id", productId, NpgsqlDbType.Integer));

        // 3. Ejecutar
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        // 4. Si no hay ninguna fila, el producto no existe -> 404
        bool found = await reader.ReadAsync(ct);
        if (!found)
        {
            throw new NotFoundException($"No existe el producto {productId}.");
        }

        return ReadProduct(reader);
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct)
    {
        string sql = "SELECT CategoryID, CategoryName, Description FROM Categories ORDER BY CategoryName";

        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);

        List<CategoryDto> result = new List<CategoryDto>();
        while (await reader.ReadAsync(ct))
        {
            CategoryDto row = new CategoryDto();
            row.CategoryId = SqlHelper.ReadInt(reader, "categoryid");
            row.CategoryName = SqlHelper.ReadString(reader, "categoryname");
            row.Description = SqlHelper.ReadNullableString(reader, "description");

            result.Add(row);
        }

        return result;
    }

    // Lee la fila actual del reader como un producto (lo usan el catálogo y el detalle)
    private static ProductDto ReadProduct(NpgsqlDataReader reader)
    {
        ProductDto product = new ProductDto();
        product.ProductId = SqlHelper.ReadInt(reader, "productid");
        product.ProductName = SqlHelper.ReadString(reader, "productname");
        product.CategoryId = SqlHelper.ReadNullableInt(reader, "categoryid");
        product.CategoryName = SqlHelper.ReadNullableString(reader, "categoryname");
        product.SupplierId = SqlHelper.ReadNullableInt(reader, "supplierid");
        product.SupplierName = SqlHelper.ReadNullableString(reader, "suppliername");
        product.Unit = SqlHelper.ReadNullableString(reader, "unit");
        product.Price = SqlHelper.ReadDecimal(reader, "price");

        return product;
    }
}
