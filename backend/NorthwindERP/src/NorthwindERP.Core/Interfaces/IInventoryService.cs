using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

/// <summary>
/// Consultas del módulo de inventario. No hay métodos para mover stock a propósito:
/// el stock solo cambia con sp_register_order y sp_delete_order (regla R8).
/// </summary>
public interface IInventoryService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? search, int? categoryId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken ct);
    Task<IReadOnlyList<StockItem>> GetStockAsync(StockFilter filter, CancellationToken ct);
    Task<IReadOnlyList<KardexEntry>> GetKardexAsync(KardexFilter filter, CancellationToken ct);
}
