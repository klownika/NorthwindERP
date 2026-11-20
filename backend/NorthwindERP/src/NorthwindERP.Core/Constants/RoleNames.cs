namespace NorthwindERP.Core.Constants;

// Nombres de los roles tal como están en la tabla Roles de la base de datos.
// Ejemplo de uso: [Authorize(Roles = RoleNames.Admin)]
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
}
