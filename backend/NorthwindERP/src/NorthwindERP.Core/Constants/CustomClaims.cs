namespace NorthwindERP.Core.Constants;

// Nombres de los datos (claims) que guardamos dentro del token JWT.
// Se usan al CREAR el token (JwtTokenService) y al LEERLO (UserClaims en la API).
public static class CustomClaims
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string Role = "role";
    public const string EmployeeId = "employeeId";
}
