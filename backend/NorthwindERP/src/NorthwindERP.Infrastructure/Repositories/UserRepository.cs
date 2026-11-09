using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly NorthwindDbContext _db;

    public UserRepository(NorthwindDbContext db) => _db = db;

    public Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.ToLower();
        return _db.UserAccounts
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, ct);
    }

    public Task<UserAccount?> GetByIdForUpdateAsync(int userAccountId, CancellationToken ct = default)
        => _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
