namespace NorthwindERP.Core.Models;

public record LoginResult(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string Username,
    string? Role,
    int EmployeeId,
    bool MustChangePassword);

public static class CustomClaims
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string Role = "role";
    public const string EmployeeId = "employeeId";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
}
