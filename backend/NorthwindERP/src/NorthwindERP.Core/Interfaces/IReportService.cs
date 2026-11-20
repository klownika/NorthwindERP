using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

// Reportes de ventas: la vista 2 (por cliente y por país) y las consultas por año
public interface IReportService
{
    Task<List<CustomerSalesDto>> GetSalesByCustomerAsync(CancellationToken ct);

    Task<List<CountrySalesDto>> GetSalesByCountryAsync(CancellationToken ct);

    Task<List<CustomerYearSalesDto>> GetSalesByCustomerYearAsync(int? year, int? customerId, CancellationToken ct);

    Task<List<EmployeeYearSalesDto>> GetSalesByEmployeeYearAsync(int? year, CancellationToken ct);
}
