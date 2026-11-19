using NorthwindERP.Core.Entities;

namespace NorthwindERP.Core.Interfaces;

// Listas de solo lectura que usa el frontend para llenar combos y pantallas:
// vendedor del pedido, transportista y proveedores.

public interface ILogisticsService
{
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Shipper>> GetShippersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken ct = default);
}

public interface ILogisticsRepository
{
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Shipper>> GetShippersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken ct = default);
}
