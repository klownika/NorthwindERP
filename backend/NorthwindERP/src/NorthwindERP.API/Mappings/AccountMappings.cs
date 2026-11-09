using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Mappings;

public static class AccountMappings
{
    public static LoginResponse ToDto(this LoginResult r) =>
        new(r.Token, r.ExpiresAtUtc, r.UserId, r.Username, r.Role, r.EmployeeId, r.MustChangePassword);
}
