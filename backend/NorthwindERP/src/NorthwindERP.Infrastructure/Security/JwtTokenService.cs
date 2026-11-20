using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.Infrastructure.Security;

// Crea el token JWT que se devuelve en el login
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TokenResult CreateToken(UserAccount user)
    {
        // 1. Fecha de vencimiento (480 minutos por defecto)
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        // 2. Datos que viajan dentro del token (claims)
        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(CustomClaims.UserId, user.UserAccountId.ToString()));
        claims.Add(new Claim(CustomClaims.Username, user.Username));
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new Claim(CustomClaims.EmployeeId, user.EmployeeId.ToString()));

        if (user.Role != null && !string.IsNullOrWhiteSpace(user.Role.RoleName))
        {
            claims.Add(new Claim(CustomClaims.Role, user.Role.RoleName));
        }

        // 3. Firmar el token con la clave secreta (Jwt:Key)
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        // 4. Convertir el token a texto (el famoso "eyJhbGci...")
        TokenResult result = new TokenResult();
        result.Token = new JwtSecurityTokenHandler().WriteToken(token);
        result.ExpiresAtUtc = expiresAtUtc;

        return result;
    }
}
