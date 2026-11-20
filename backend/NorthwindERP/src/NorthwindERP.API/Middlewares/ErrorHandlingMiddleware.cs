using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NorthwindERP.Core.Exceptions;

namespace NorthwindERP.API.Middlewares;

// Middleware = un filtro por el que pasa TODA petición.
// Este envuelve todo en un try/catch: si un servicio lanza una excepción,
// aquí se convierte en una respuesta JSON con el código HTTP correcto.
// Así los controladores no necesitan try/catch.
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Dejar pasar la petición al siguiente paso (controlador, servicio...)
            await _next(context);
        }
        catch (Exception ex)
        {
            // Si ya se empezó a enviar la respuesta, no se puede cambiar: relanzar
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // 1. Traducir la excepción a un ProblemDetails (formato estándar de error)
        ProblemDetails problem = BuildProblem(ex);
        problem.Instance = context.Request.Path;

        int status = problem.Status ?? StatusCodes.Status500InternalServerError;

        // 2. Registrar en la consola: los 500 son errores nuestros, el resto son peticiones rechazadas
        if (status >= 500)
        {
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Petición rechazada ({Status}) en {Path}: {Detail}", status, context.Request.Path, problem.Detail);
        }

        // 3. Escribir la respuesta JSON
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private ProblemDetails BuildProblem(Exception ex)
    {
        ProblemDetails problem = new ProblemDetails();

        // --- Nuestras excepciones (las lanzan los servicios) ---
        if (ex is NotFoundException)
        {
            problem.Status = StatusCodes.Status404NotFound;
            problem.Title = "Not found";
            problem.Detail = ex.Message;
            return problem;
        }

        if (ex is BusinessRuleException)
        {
            problem.Status = StatusCodes.Status400BadRequest;
            problem.Title = "Business rule violated";
            problem.Detail = ex.Message;
            return problem;
        }

        if (ex is UnauthorizedAccessException)
        {
            problem.Status = StatusCodes.Status401Unauthorized;
            problem.Title = "Unauthorized";
            problem.Detail = ex.Message;
            return problem;
        }

        // --- Errores de PostgreSQL (a veces vienen dentro de otra excepción) ---
        PostgresException? pg = ex as PostgresException;
        if (pg == null)
        {
            pg = ex.InnerException as PostgresException;
        }

        if (pg != null)
        {
            problem.Extensions["code"] = pg.SqlState;
            FillPostgresProblem(problem, pg);
            return problem;
        }

        // --- Cualquier otra cosa: error 500 (el detalle solo se muestra en Development) ---
        problem.Status = StatusCodes.Status500InternalServerError;
        problem.Title = "Internal server error";
        if (_env.IsDevelopment())
        {
            problem.Detail = ex.Message;
        }

        return problem;
    }

    private void FillPostgresProblem(ProblemDetails problem, PostgresException pg)
    {
        switch (pg.SqlState)
        {
            // Códigos propios que lanzan nuestros procedimientos: su mensaje es seguro de mostrar
            case "NW400":
                problem.Status = 400;
                problem.Title = "Invalid request";
                problem.Detail = pg.MessageText;
                break;

            case "NW404":
                problem.Status = 404;
                problem.Title = "Resource not found";
                problem.Detail = pg.MessageText;
                break;

            case "NW409":
                problem.Status = 409;
                problem.Title = "Insufficient stock";
                problem.Detail = pg.MessageText;
                break;

            // Errores estándar de PostgreSQL
            case PostgresErrorCodes.ForeignKeyViolation:
                problem.Status = 409;
                problem.Title = "Related record conflict";
                break;

            case PostgresErrorCodes.UniqueViolation:
                problem.Status = 409;
                problem.Title = "Duplicate value";
                break;

            case PostgresErrorCodes.NotNullViolation:
                problem.Status = 400;
                problem.Title = "Missing required value";
                break;

            case PostgresErrorCodes.CheckViolation:
                problem.Status = 400;
                problem.Title = "Value out of range";
                break;

            case PostgresErrorCodes.StringDataRightTruncation:
                problem.Status = 400;
                problem.Title = "Value too long";
                break;

            default:
                problem.Status = 500;
                problem.Title = "Database error";
                if (_env.IsDevelopment())
                {
                    problem.Detail = pg.MessageText;
                }
                break;
        }
    }
}
