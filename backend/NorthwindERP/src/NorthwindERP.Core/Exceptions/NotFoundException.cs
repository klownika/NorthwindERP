namespace NorthwindERP.Core.Exceptions;

// Se lanza cuando algo no existe (un cliente, un producto...).
// El ErrorHandlingMiddleware la convierte en una respuesta 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
