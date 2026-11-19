using Npgsql;
using NpgsqlTypes;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;
using static NorthwindERP.Infrastructure.Data.DbHelpers;

namespace NorthwindERP.Infrastructure.Services;

/// <summary>Consulta de un producto del catálogo con SQL directo (Npgsql).</summary>
public sealed class ProductService : IProductService
{
    private readonly NpgsqlDataSource _ds;

    public ProductService(NpgsqlDataSource ds) => _ds = ds;

    // LEFT JOIN porque en Northwind la categoría y el proveedor pueden venir vacíos
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
}
