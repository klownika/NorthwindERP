using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.Infrastructure.Security;

// Implementación de IPasswordHasher con la librería BCrypt.Net-Next
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception)
        {
            // Si el hash guardado está mal formado, lo tratamos como contraseña incorrecta
            return false;
        }
    }
}
