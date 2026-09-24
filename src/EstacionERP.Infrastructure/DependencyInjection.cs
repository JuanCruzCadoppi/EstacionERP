using EstacionERP.Application.Common;
using EstacionERP.Application.Facturacion;
using EstacionERP.Infrastructure.Arca;
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

        // Web services de ARCA.
        services.AddHttpClient<WsaaClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<IArcaClient, ArcaClient>(c => c.Timeout = TimeSpan.FromSeconds(40));
        return services;
    }
}
