using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Repositories;

// Consultas simples con EF Core. AsNoTracking porque solo se leen,
// nunca se modifican desde aquí.
public class LogisticsRepository : ILogisticsRepository
{
    private readonly NorthwindDbContext _db;

    public LogisticsRepository(NorthwindDbContext db) => _db = db;

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct = default)
        => await _db.Employees.AsNoTracking()
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Shipper>> GetShippersAsync(CancellationToken ct = default)
        => await _db.Shippers.AsNoTracking().OrderBy(s => s.ShipperName).ToListAsync(ct);

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken ct = default)
        => await _db.Suppliers.AsNoTracking().OrderBy(s => s.SupplierName).ToListAsync(ct);
}
