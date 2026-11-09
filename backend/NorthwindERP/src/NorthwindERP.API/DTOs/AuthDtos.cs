using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.API.DTOs;

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string Username,
    string? Role,
    int EmployeeId,
    bool MustChangePassword);
