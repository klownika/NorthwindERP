using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;
using NorthwindERP.Infrastructure.Repositories;
using NorthwindERP.Infrastructure.Security;

namespace NorthwindERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Supabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Supabase no está configurada (usa dotnet user-secrets).");

        services.AddDbContext<NorthwindDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseLowerCaseNamingConvention());

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
