using EstacionERP.Application.Common;
using EstacionERP.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra la base de datos PostgreSQL.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EstacionDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IEstacionDbContext>(sp => sp.GetRequiredService<EstacionDbContext>());
        return services;
    }
}
