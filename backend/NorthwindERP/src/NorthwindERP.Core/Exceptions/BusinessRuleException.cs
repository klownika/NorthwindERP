namespace NorthwindERP.Core.Exceptions;

// Se lanza cuando el pedido del usuario rompe una regla del negocio
// (por ejemplo, un nombre vacío o una contraseña actual incorrecta).
// El ErrorHandlingMiddleware la convierte en una respuesta 400.
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
