using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

// Consultas de inventario y catálogo. No hay métodos para mover stock a propósito:
// el stock solo cambia con sp_register_order y sp_delete_order (regla R8).
public interface IInventoryService
{
    Task<List<StockDto>> GetStockAsync(int? warehouseId, int? productId, bool lowStockOnly, CancellationToken ct);

    Task<List<KardexEntryDto>> GetKardexAsync(int? productId, int? warehouseId, string? type, DateTime? from, DateTime? to, int take, CancellationToken ct);

    Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken ct);

    Task<List<ProductDto>> GetProductsAsync(string? search, int? categoryId, CancellationToken ct);

    // Lanza NotFoundException (404) si el producto no existe
    Task<ProductDto> GetProductByIdAsync(int productId, CancellationToken ct);

    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct);
}
