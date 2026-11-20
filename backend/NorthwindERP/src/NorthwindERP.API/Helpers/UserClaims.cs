using System.Security.Claims;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.Exceptions;

namespace NorthwindERP.API.Helpers;

// Lee los datos del usuario que vienen dentro del token JWT.
// En un controlador, "User" es el usuario del token. Ejemplo:
//   int employeeId = UserClaims.GetEmployeeId(User);
public static class UserClaims
{
    // Id de la cuenta (claim "sub"). Si falta, el token es inválido -> 401
    public static int GetUserId(ClaimsPrincipal user)
    {
        string? value = user.FindFirst(CustomClaims.UserId)?.Value;

        int userId;
        if (int.TryParse(value, out userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Token inválido.");
    }

    // Id del empleado (claim "employeeId"). Es obligatorio para crear o borrar pedidos -> 400 si falta
    public static int GetEmployeeId(ClaimsPrincipal user)
    {
        int? employeeId = GetEmployeeIdOrNull(user);

        if (employeeId == null)
        {
            throw new BusinessRuleException("Tu cuenta no tiene un empleado asociado.");
        }

        return employeeId.Value;
    }

    // Igual que GetEmployeeId, pero devuelve null en vez de lanzar error
    public static int? GetEmployeeIdOrNull(ClaimsPrincipal user)
    {
        string? value = user.FindFirst(CustomClaims.EmployeeId)?.Value;

        int employeeId;
        if (int.TryParse(value, out employeeId))
        {
            return employeeId;
        }

        return null;
    }

    public static string? GetUsername(ClaimsPrincipal user)
    {
        return user.FindFirst(CustomClaims.Username)?.Value;
    }

    public static string? GetRole(ClaimsPrincipal user)
    {
        return user.FindFirst(CustomClaims.Role)?.Value;
    }
}
