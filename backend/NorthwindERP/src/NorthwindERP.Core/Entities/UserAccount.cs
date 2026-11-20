namespace NorthwindERP.Core.Entities;

// Una fila de la tabla "useraccounts": las cuentas para iniciar sesión.
public class UserAccount
{
    public int UserAccountId { get; set; }
    public int EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    // true = el usuario debe cambiar su contraseña al entrar (columna changepassword)
    public bool MustChangePassword { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Propiedades de navegación: EF las llena cuando usas .Include(...)
    public Role? Role { get; set; }
    public Employee? Employee { get; set; }
}
