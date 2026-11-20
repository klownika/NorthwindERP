using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.Infrastructure.Data;

// El DbContext es el "puente" entre C# y la base de datos para EF Core.
// Cada DbSet es una tabla: _db.Customers.ToListAsync() hace un SELECT a customers.
// Solo se usa para las tablas simples (cuentas, clientes, logística).
// Inventario, pedidos y reportes usan SQL directo con Npgsql (ver Services/).
public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles
    {
        get { return Set<Role>(); }
    }

    public DbSet<UserAccount> UserAccounts
    {
        get { return Set<UserAccount>(); }
    }

    public DbSet<Employee> Employees
    {
        get { return Set<Employee>(); }
    }

    public DbSet<Customer> Customers
    {
        get { return Set<Customer>(); }
    }

    public DbSet<Shipper> Shippers
    {
        get { return Set<Shipper>(); }
    }

    public DbSet<Supplier> Suppliers
    {
        get { return Set<Supplier>(); }
    }

    // Aquí se dice a qué tabla de PostgreSQL va cada entidad.
    // Los nombres de columna los pone UseLowerCaseNamingConvention() en Program.cs
    // (CustomerName -> customername).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserAccount>().ToTable("useraccounts");
        modelBuilder.Entity<Employee>().ToTable("employees");
        modelBuilder.Entity<Customer>().ToTable("customers");
        modelBuilder.Entity<Shipper>().ToTable("shippers");
        modelBuilder.Entity<Supplier>().ToTable("suppliers");
    }
}
