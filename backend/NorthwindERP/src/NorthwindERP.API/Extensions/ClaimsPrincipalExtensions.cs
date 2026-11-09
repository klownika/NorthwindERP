using System.Security.Claims;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(CustomClaims.EmployeeId)?.Value;

        return int.TryParse(value, out var employeeId)
            ? employeeId
            : throw new BusinessRuleException("Tu cuenta no tiene un empleado asociado.");
    }

    public static int? GetEmployeeIdOrNull(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst(CustomClaims.EmployeeId)?.Value, out var id) ? id : null;

    public static int GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst(CustomClaims.UserId)?.Value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Token inválido.");
}
