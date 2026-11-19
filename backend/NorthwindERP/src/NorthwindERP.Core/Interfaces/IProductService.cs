using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

/// <summary>
/// Alta y edición del catálogo. Separado de IInventoryService porque ese es solo de
/// lectura; aquí se toca la tabla Products, pero nunca la cantidad en stock.
/// No hay borrado: un producto con ventas o movimientos en el Kardex no se puede eliminar.
/// </summary>
public interface IProductService
{
    Task<Product> GetByIdAsync(int productId, CancellationToken ct);
    Task<Product> CreateAsync(NewProduct product, CancellationToken ct);
    Task<Product> UpdateAsync(int productId, ProductUpdate update, CancellationToken ct);
}
