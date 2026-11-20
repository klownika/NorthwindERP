using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.API.Controllers;

/// <summary>Estado de la API</summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous] // no pide token: sirve para revisar rápido si hay conexión
public class HealthController : ControllerBase
{
    private readonly NorthwindDbContext _db;

    public HealthController(NorthwindDbContext db)
    {
        _db = db;
    }

    /// <summary>Responde 200 si la API puede conectarse a Supabase, o 503 si no.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool canConnect = await _db.Database.CanConnectAsync(ct);

        if (canConnect)
        {
            return Ok(new { status = "ok", database = "connected" });
        }

        return Problem(
            detail: "No se pudo conectar a la base de datos. Revisa ConnectionStrings:Supabase.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
