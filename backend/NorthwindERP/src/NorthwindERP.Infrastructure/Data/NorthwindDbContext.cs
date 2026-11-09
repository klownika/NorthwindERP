using Microsoft.EntityFrameworkCore;

namespace NorthwindERP.Infrastructure.Data;

public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options) { }
}
