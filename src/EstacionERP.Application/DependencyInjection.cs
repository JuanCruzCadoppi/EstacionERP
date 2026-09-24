using EstacionERP.Application.Clientes;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios (casos de uso) de la capa Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClienteService, ClienteService>();
        return services;
    }
}
