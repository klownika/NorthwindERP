using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NorthwindERP.Core.Exceptions;

namespace NorthwindERP.API.Middlewares;

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
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail, code) = Map(ex);

        if (status >= 500)
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
        else
            _logger.LogWarning("Petición rechazada ({Status}) en {Path}: {Detail}", status, context.Request.Path, detail);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        if (code is not null) problem.Extensions["code"] = code;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private (int Status, string Title, string? Detail, string? Code) Map(Exception ex)
    {
        switch (ex)
        {
            case NotFoundException:
                return (StatusCodes.Status404NotFound, "Not found", ex.Message, null);
            case BusinessRuleException:
                return (StatusCodes.Status400BadRequest, "Business rule violated", ex.Message, null);
            case UnauthorizedAccessException:
                return (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message, null);
        }

        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        if (pg is not null)
        {
            return pg.SqlState switch
            {
                PostgresErrorCodes.ForeignKeyViolation => (409, "Related record conflict", null, pg.SqlState),
                PostgresErrorCodes.UniqueViolation => (409, "Duplicate value", null, pg.SqlState),
                PostgresErrorCodes.NotNullViolation => (400, "Missing required value", null, pg.SqlState),
                PostgresErrorCodes.CheckViolation => (400, "Value out of range", null, pg.SqlState),
                PostgresErrorCodes.StringDataRightTruncation => (400, "Value too long", null, pg.SqlState),
                _ => (500, "Database error", _env.IsDevelopment() ? pg.MessageText : null, pg.SqlState)
            };
        }

        return (StatusCodes.Status500InternalServerError, "Internal server error",
            _env.IsDevelopment() ? ex.Message : null, null);
    }
}
