namespace NorthwindERP.Core.Interfaces;

// Convierte contraseñas en hash (BCrypt) y las compara.
// Nunca se guarda la contraseña en texto plano.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
