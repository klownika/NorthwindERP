using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using NorthwindERP.API.Middlewares;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;
using NorthwindERP.Infrastructure.Security;
using NorthwindERP.Infrastructure.Services;

// Program.cs tiene dos partes:
//   PARTE 1 (builder.Services...): REGISTRAR todo lo que la API necesita.
//   PARTE 2 (app.Use...):          el PIPELINE, los filtros por los que pasa cada petición.

// Las fechas de Northwind no tienen zona horaria; esto permite leerlas sin error en PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// =====================================================================
// PARTE 1: REGISTRAR SERVICIOS
// =====================================================================

// ---- 1.1 Controladores: busca todas las clases *Controller ----
builder.Services.AddControllers();

// ---- 1.2 Base de datos ----
// La cadena de conexión sale de user-secrets: ConnectionStrings:Supabase
string? connectionString = builder.Configuration.GetConnectionString("Supabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Supabase no está configurada (usa dotnet user-secrets).");
}

// EF Core: para las tablas simples (cuentas, clientes, logística)
builder.Services.AddDbContext<NorthwindDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseLowerCaseNamingConvention(); // CustomerName -> customername
});

// Npgsql directo: para procedimientos, vistas y consultas con SQL escrito a mano
NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);

// ---- 1.3 Seguridad: contraseñas y tokens ----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// ---- 1.4 Servicios de la aplicación (inyección de dependencias) ----
// Se lee así: "cuando un controlador pida ICustomerService, dale un CustomerService".
// AddScoped = se crea uno nuevo por cada petición HTTP.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILogisticsService, LogisticsService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ---- 1.5 Autenticación con JWT ----
JwtSettings? jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres (usa dotnet user-secrets).");
}

byte[] signingKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantener los nombres de claims tal cual ("sub", "role", "employeeId")
        options.MapInboundClaims = false;

        // Qué revisa la API en cada token que llega
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = CustomClaims.Username,
            RoleClaimType = CustomClaims.Role
        };
    });

builder.Services.AddAuthorization();

// ---- 1.6 Swagger con el botón "Authorize" para pegar el token ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NorthwindERP API",
        Version = "v1",
        Description = "Backend del ERP de inventario Northwind (ASP.NET Core + Supabase)."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí solo el token que devuelve /api/auth/login (sin la palabra Bearer)."
    });

    OpenApiSecurityScheme bearerReference = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    OpenApiSecurityRequirement requirement = new OpenApiSecurityRequirement();
    requirement.Add(bearerReference, Array.Empty<string>());
    options.AddSecurityRequirement(requirement);

    // Usa los comentarios /// <summary> de los controladores como descripción en Swagger
    string xmlFile = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ---- 1.7 CORS: qué páginas web (orígenes) pueden llamar a la API ----
// Los orígenes salen de Cors:AllowedOrigins en appsettings.json (Angular: http://localhost:4200)
const string FrontendCorsPolicy = "Frontend";

string[]? configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
string[] allowedOrigins = configuredOrigins ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

WebApplication app = builder.Build();

// =====================================================================
// PARTE 2: PIPELINE (el ORDEN importa: cada petición pasa por aquí de arriba abajo)
// =====================================================================

// Primero: atrapa cualquier excepción de las capas de abajo y la convierte en JSON (400, 404, 409, 500...)
app.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger en /swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "NorthwindERP API";
});

app.UseHttpsRedirection();

// CORS va antes de la autenticación: el preflight (OPTIONS) del navegador llega sin token
app.UseCors(FrontendCorsPolicy);

// Autenticación (¿quién eres? lee el token) y luego autorización (¿puedes entrar aquí?)
app.UseAuthentication();
app.UseAuthorization();

// Conecta las rutas de los controladores ([Route("api/...")])
app.MapControllers();

// Crea el usuario admin si todavía no existe
await AdminSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

app.Run();

// Permite usar la clase Program en pruebas de integración
public partial class Program
{
}
