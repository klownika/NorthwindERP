using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.Infrastructure.Data;

public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserAccount>().ToTable("useraccounts");
        modelBuilder.Entity<Employee>().ToTable("employees");
    }
}
