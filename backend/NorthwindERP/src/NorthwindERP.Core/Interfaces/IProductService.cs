using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

/// <summary>
/// Consulta de un producto por id. Por ahora el catálogo es de solo lectura:
/// el alta y la edición de productos quedan para más adelante.
/// </summary>
public interface IProductService
{
    Task<Product> GetByIdAsync(int productId, CancellationToken ct);
}
