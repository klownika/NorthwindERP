using NorthwindERP.API.Extensions;
using NorthwindERP.API.Middlewares;
using NorthwindERP.Infrastructure;
using NorthwindERP.Infrastructure.Data;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "NorthwindERP API");
app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/api/health", async (NorthwindDbContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "ok", database = "connected" })
            : Results.Problem("No se pudo conectar a la base de datos. Revisa ConnectionStrings:Supabase.", statusCode: 503))
    .AllowAnonymous()
    .WithTags("Health");

app.Run();

public partial class Program { }
