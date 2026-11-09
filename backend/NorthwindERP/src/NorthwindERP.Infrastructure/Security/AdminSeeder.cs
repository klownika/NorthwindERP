using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Core.Models;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Security;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(this IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed:AdminPassword no está configurada; se omite la creación del admin.");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == Roles.Admin);
        if (adminRole is null)
        {
            logger.LogWarning("No existe el rol 'Admin'. Ejecuta primero los scripts de la carpeta database.");
            return;
        }

        if (await db.UserAccounts.AnyAsync(u => u.RoleId == adminRole.RoleId))
            return;

        db.UserAccounts.Add(new UserAccount
        {
            EmployeeId = int.TryParse(config["Seed:AdminEmployeeId"], out var employeeId) ? employeeId : 2,
            Username = config["Seed:AdminUsername"] ?? "admin",
            PasswordHash = hasher.Hash(password),
            RoleId = adminRole.RoleId,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Cuenta de administrador inicial creada (debe cambiar la contraseña al primer ingreso).");
    }
}
