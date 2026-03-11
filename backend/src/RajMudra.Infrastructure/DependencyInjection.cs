using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Infrastructure.Persistence;
using RajMudra.Infrastructure.Services;

namespace RajMudra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
        }

        services.AddDbContext<RajMudraDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        return services;
    }
}

