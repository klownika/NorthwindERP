using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

// Listas de solo lectura que usa el frontend para llenar combos y pantallas
public interface ILogisticsService
{
    Task<List<EmployeeDto>> GetEmployeesAsync(CancellationToken ct);

    Task<List<ShipperDto>> GetShippersAsync(CancellationToken ct);

    Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct);
}
