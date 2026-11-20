using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NorthwindERP.Core.Constants;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Security;

// Crea la cuenta "admin" la primera vez que arranca la API, si todavía no existe.
// La contraseña sale de Seed:AdminPassword (dotnet user-secrets).
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        // 1. Sin contraseña configurada no se crea nada
        string? password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed:AdminPassword no está configurada; se omite la creación del admin.");
            return;
        }

        // 2. Pedimos el DbContext y el hasher al contenedor de servicios
        using IServiceScope scope = services.CreateScope();
        NorthwindDbContext db = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // 3. Buscamos el rol Admin (lo crean los scripts de la carpeta database)
        Role? adminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleNames.Admin);
        if (adminRole == null)
        {
            logger.LogWarning("No existe el rol 'Admin'. Ejecuta primero los scripts de la carpeta database.");
            return;
        }

        // 4. Si ya hay un usuario Admin, no hacemos nada
        bool adminExists = await db.UserAccounts.AnyAsync(u => u.RoleId == adminRole.RoleId);
        if (adminExists)
        {
            return;
        }

        // 5. Datos del admin desde appsettings (Seed:AdminUsername y Seed:AdminEmployeeId)
        int employeeId = 2;
        int configuredEmployeeId;
        if (int.TryParse(config["Seed:AdminEmployeeId"], out configuredEmployeeId))
        {
            employeeId = configuredEmployeeId;
        }

        string username = config["Seed:AdminUsername"] ?? "admin";

        // 6. Crear la cuenta y guardarla
        UserAccount admin = new UserAccount();
        admin.EmployeeId = employeeId;
        admin.Username = username;
        admin.PasswordHash = hasher.Hash(password);
        admin.RoleId = adminRole.RoleId;
        admin.IsActive = true;
        admin.MustChangePassword = true;
        admin.CreatedAt = DateTime.Now;

        db.UserAccounts.Add(admin);
        await db.SaveChangesAsync();

        logger.LogInformation("Cuenta de administrador inicial creada (debe cambiar la contraseña al primer ingreso).");
    }
}
