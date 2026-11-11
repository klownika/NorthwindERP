using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly NorthwindDbContext _db;

    public CustomerRepository(NorthwindDbContext db) => _db = db;

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
        => await _db.Customers.AsNoTracking().OrderBy(c => c.CustomerName).ToListAsync(ct);

    // Con tracking, para que EF detecte los cambios al actualizar
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
