using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

/// <summary>
/// Reportes de ventas: la vista 2 del enunciado (por cliente y por región) y las
/// consultas de ventas por cliente y por empleado con totales por año.
/// </summary>
public interface IReportService
{
    Task<IReadOnlyList<CustomerSales>> GetSalesByCustomerAsync(CancellationToken ct);
    Task<IReadOnlyList<CountrySales>> GetSalesByCountryAsync(CancellationToken ct);
    Task<IReadOnlyList<CustomerYearSales>> GetSalesByCustomerYearAsync(int? year, int? customerId, CancellationToken ct);
    Task<IReadOnlyList<EmployeeYearSales>> GetSalesByEmployeeYearAsync(int? year, CancellationToken ct);
}
