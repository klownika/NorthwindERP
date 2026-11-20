using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// Login y cambio de contraseña. Usa EF Core sobre la tabla useraccounts.
public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Usuario o contraseña incorrectos.";

    private readonly NorthwindDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(NorthwindDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct)
    {
        // 1. Buscar el usuario sin importar mayúsculas ("Admin" = "admin"), junto con su rol
        string normalizedUsername = username.Trim().ToLower();

        UserAccount? user = await _db.UserAccounts
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername, ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        // 2. Comparar la contraseña con el hash guardado
        bool passwordIsCorrect = _hasher.Verify(password, user.PasswordHash);
        if (!passwordIsCorrect)
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        // 3. La cuenta tiene que estar activa
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta de usuario está desactivada.");
        }

        // 4. Crear el token y armar la respuesta
        TokenResult token = _tokens.CreateToken(user);

        LoginResponse response = new LoginResponse();
        response.Token = token.Token;
        response.ExpiresAtUtc = token.ExpiresAtUtc;
        response.UserId = user.UserAccountId;
        response.Username = user.Username;
        response.Role = user.Role?.RoleName;
        response.EmployeeId = user.EmployeeId;
        response.ChangePassword = user.ChangePassword;

        return response;
    }

    public async Task ChangePasswordAsync(int userAccountId, string currentPassword, string newPassword, CancellationToken ct)
    {
        // 1. Buscar el usuario del token (con tracking, porque lo vamos a modificar)
        UserAccount? user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId, ct);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Token inválido.");
        }

        // 2. Validar la contraseña actual y que la nueva sea distinta
        if (!_hasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("La contraseña actual es incorrecta.");
        }

        if (currentPassword == newPassword)
        {
            throw new BusinessRuleException("La nueva contraseña debe ser distinta de la actual.");
        }

        // 3. Guardar el nuevo hash y apagar ChangePassword (EF genera el UPDATE)
        user.PasswordHash = _hasher.Hash(newPassword);
        user.ChangePassword = false;

        await _db.SaveChangesAsync(ct);
    }
}
