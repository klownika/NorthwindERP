using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.DTOs;
using NorthwindERP.API.Extensions;
using NorthwindERP.API.Mappings;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Controllers;

/// <summary>Cuentas: autenticación</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>
    /// Valida usuario y contraseña y devuelve un token JWT.
    /// Si MustChangePassword es true, el frontend debe llevar al usuario a cambiar la contraseña.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request.Username, request.Password, ct);
        return Ok(result.ToDto());
    }

    /// <summary>Devuelve los datos del usuario dueño del token.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserDto> Me()
        => Ok(new CurrentUserDto(
            User.GetUserId(),
            User.FindFirst(CustomClaims.Username)?.Value,
            User.FindFirst(CustomClaims.Role)?.Value,
            User.GetEmployeeIdOrNull()));
}
