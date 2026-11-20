using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.Core.Interfaces;

// Crea el token JWT que el usuario manda en cada petición (Authorization: Bearer ...)
public interface ITokenService
{
    TokenResult CreateToken(UserAccount user);
}
