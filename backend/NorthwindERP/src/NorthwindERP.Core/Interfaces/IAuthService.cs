using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

// Interfaz = la lista de lo que el servicio sabe hacer, sin el "cómo".
// El controlador solo conoce la interfaz; la clase real (AuthService) la elige Program.cs.
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct);

    Task ChangePasswordAsync(int userAccountId, string currentPassword, string newPassword, CancellationToken ct);
}
