using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.API.Helpers;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.API.Controllers;

// El controlador solo hace 3 cosas: RECIBIR la petición, LLAMAR al servicio y RESPONDER.
// Las reglas del negocio están en el servicio (Infrastructure/Services/AuthService.cs).

/// <summary>Cuentas: autenticación</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // ASP.NET entrega el servicio aquí (inyección de dependencias, registrado en Program.cs)
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Valida usuario y contraseña y devuelve un token JWT.
    /// Si changePassword es true, el frontend debe llevar al usuario a cambiar la contraseña.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // Si las credenciales son incorrectas, el servicio lanza una excepción -> 401
        LoginResponse response = await _authService.LoginAsync(request.Username, request.Password, ct);
        return Ok(response);
    }

    /// <summary>Devuelve los datos del usuario dueño del token.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserDto> Me()
    {
        // No consulta la base de datos: todo viene dentro del token
        CurrentUserDto currentUser = new CurrentUserDto();
        currentUser.UserId = UserClaims.GetUserId(User);
        currentUser.Username = UserClaims.GetUsername(User);
        currentUser.Role = UserClaims.GetRole(User);
        currentUser.EmployeeId = UserClaims.GetEmployeeIdOrNull(User);

        return Ok(currentUser);
    }

    /// <summary>Cambia la contraseña del usuario del token.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        int userId = UserClaims.GetUserId(User);

        await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);

        // 204 = salió bien, sin contenido que devolver
        return NoContent();
    }
}
