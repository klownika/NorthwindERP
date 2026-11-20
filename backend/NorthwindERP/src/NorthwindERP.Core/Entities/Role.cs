namespace NorthwindERP.Core.Entities;

// Una fila de la tabla "roles" (Admin, Employee).
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
