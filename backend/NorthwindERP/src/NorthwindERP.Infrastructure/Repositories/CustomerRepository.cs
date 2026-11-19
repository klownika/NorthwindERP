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

    // El INSERT recién se manda en SaveChangesAsync; ahí PostgreSQL genera el
    // CustomerID con la secuencia SERIAL y EF lo deja cargado en la entidad
    public void Add(Customer customer) => _db.Customers.Add(customer);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
