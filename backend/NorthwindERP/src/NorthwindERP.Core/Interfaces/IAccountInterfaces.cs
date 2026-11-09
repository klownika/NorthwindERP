using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Models;

namespace NorthwindERP.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccount user);
}
