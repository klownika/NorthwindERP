using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;
using NorthwindERP.Infrastructure.Repositories;
using NorthwindERP.Infrastructure.Security;
using NorthwindERP.Infrastructure.Services;

namespace NorthwindERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Supabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Supabase no está configurada (usa dotnet user-secrets).");

        // EF Core para las tablas simples (cuentas, clientes, logística)
        services.AddDbContext<NorthwindDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseLowerCaseNamingConvention());

        // Npgsql directo para procedimientos, vistas y consultas con SQL a mano
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILogisticsRepository, LogisticsRepository>();

        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
