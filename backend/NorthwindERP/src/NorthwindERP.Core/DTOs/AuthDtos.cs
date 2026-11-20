using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.Core.DTOs;

// DTO = "Data Transfer Object": la forma del JSON que ENTRA o SALE de la API.
// Las etiquetas [Required], [MinLength]... las revisa ASP.NET antes de entrar
// al controlador. Si fallan, responde 400 automáticamente.

// ENTRA en POST /api/auth/login
public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// SALE de POST /api/auth/login
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int EmployeeId { get; set; }
    public bool ChangePassword { get; set; }
}

// SALE de GET /api/auth/me
public class CurrentUserDto
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public int? EmployeeId { get; set; }
}

// ENTRA en POST /api/auth/change-password
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

// Uso interno (no sale en el JSON): lo que devuelve ITokenService al crear un token
public class TokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
