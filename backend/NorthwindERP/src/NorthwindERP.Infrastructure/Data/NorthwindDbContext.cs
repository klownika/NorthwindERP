using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.Infrastructure.Data;

public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Shipper> Shippers => Set<Shipper>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

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
