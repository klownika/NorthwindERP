using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;
using static NorthwindERP.Infrastructure.Data.DbHelpers;

namespace NorthwindERP.Infrastructure.Services;

/// <summary>Alta y edición de productos con SQL directo (Npgsql).</summary>
public sealed class ProductService : IProductService
{
    private readonly NpgsqlDataSource _ds;

    public ProductService(NpgsqlDataSource ds) => _ds = ds;

    public async Task<Product> GetByIdAsync(int productId, CancellationToken ct)
    {
        var rows = await _ds.QueryAsync(
            """
            SELECT p.ProductID, p.ProductName, p.CategoryID, c.CategoryName,
                   p.SupplierID, s.SupplierName, p.Unit, p.Price
            FROM Products p
            LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
            LEFT JOIN Suppliers s  ON s.SupplierID = p.SupplierID
            WHERE p.ProductID = @id
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
            P("id", productId, NpgsqlDbType.Integer));

        return rows.Count > 0 ? rows[0] : throw new NotFoundException($"No existe el producto {productId}.");
    }

    public async Task<Product> CreateAsync(NewProduct product, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new BusinessRuleException("El nombre del producto es obligatorio.");

        await using var conn = await _ds.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await EnsureCategoryAndSupplierAsync(conn, tx, product.CategoryId, product.SupplierId, ct);

        int productId;
        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO Products (ProductName, SupplierID, CategoryID, Unit, Price)
            VALUES (@name, @supplier, @category, @unit, @price)
            RETURNING ProductID
            """, conn, tx))
        {
            insert.Parameters.Add(P("name", product.ProductName.Trim(), NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("supplier", product.SupplierId, NpgsqlDbType.Integer));
            insert.Parameters.Add(P("category", product.CategoryId, NpgsqlDbType.Integer));
            insert.Parameters.Add(P("unit", product.Unit?.Trim(), NpgsqlDbType.Varchar));
            insert.Parameters.Add(P("price", product.Price, NpgsqlDbType.Numeric));
            productId = Convert.ToInt32(await insert.ExecuteScalarAsync(ct));
        }

        // sp_register_order busca la fila (almacén, producto) en InventoryStock; si no
        // existe, el producto nuevo no se podría vender ni saldría en vw_stock_status.
        // Se crea con 0 en cada almacén: sin movimientos, el Kardex también da 0 y
        // KardexOk queda en true, así que no se rompe la regla R8.
        await using (var stock = new NpgsqlCommand(
            """
            INSERT INTO InventoryStock (WarehouseID, ProductID, Quantity, MinStockLevel)
            SELECT WarehouseID, @id, 0, 0 FROM Warehouses
            """, conn, tx))
        {
            stock.Parameters.Add(P("id", productId, NpgsqlDbType.Integer));
            await stock.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return await GetByIdAsync(productId, ct);
    }

    public async Task<Product> UpdateAsync(int productId, ProductUpdate update, CancellationToken ct)
    {
        var nothingToUpdate =
            string.IsNullOrWhiteSpace(update.ProductName) &&
            update.CategoryId is null &&
            update.SupplierId is null &&
            string.IsNullOrWhiteSpace(update.Unit) &&
            update.Price is null;

        if (nothingToUpdate)
            throw new BusinessRuleException("Envía al menos un campo para actualizar.");

        await using var conn = await _ds.OpenConnectionAsync(ct);
        await EnsureCategoryAndSupplierAsync(conn, null, update.CategoryId, update.SupplierId, ct);

        // COALESCE deja el valor actual cuando el parámetro llega null
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE Products
            SET ProductName = COALESCE(@name, ProductName),
                CategoryID  = COALESCE(@category, CategoryID),
                SupplierID  = COALESCE(@supplier, SupplierID),
                Unit        = COALESCE(@unit, Unit),
                Price       = COALESCE(@price, Price)
            WHERE ProductID = @id
            """, conn);
        cmd.Parameters.Add(P("name", Clean(update.ProductName), NpgsqlDbType.Varchar));
        cmd.Parameters.Add(P("category", update.CategoryId, NpgsqlDbType.Integer));
        cmd.Parameters.Add(P("supplier", update.SupplierId, NpgsqlDbType.Integer));
        cmd.Parameters.Add(P("unit", Clean(update.Unit), NpgsqlDbType.Varchar));
        cmd.Parameters.Add(P("price", update.Price, NpgsqlDbType.Numeric));
        cmd.Parameters.Add(P("id", productId, NpgsqlDbType.Integer));

        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            throw new NotFoundException($"No existe el producto {productId}.");

        return await GetByIdAsync(productId, ct);
    }

    // Se revisa antes del INSERT/UPDATE para devolver un 400 con un mensaje claro,
    // en vez del 409 genérico que daría la FK
    private static async Task EnsureCategoryAndSupplierAsync(
        NpgsqlConnection conn, NpgsqlTransaction? tx, int? categoryId, int? supplierId, CancellationToken ct)
    {
        if (categoryId is not null && !await ExistsAsync(conn, tx, "SELECT 1 FROM Categories WHERE CategoryID = @id", categoryId.Value, ct))
            throw new BusinessRuleException($"No existe la categoría {categoryId}.");

        if (supplierId is not null && !await ExistsAsync(conn, tx, "SELECT 1 FROM Suppliers WHERE SupplierID = @id", supplierId.Value, ct))
            throw new BusinessRuleException($"No existe el proveedor {supplierId}.");
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql, int id, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.Add(P("id", id, NpgsqlDbType.Integer));
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
