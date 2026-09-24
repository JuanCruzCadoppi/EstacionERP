using EstacionERP.Application.Clientes;
using EstacionERP.Application.Configuracion;
using EstacionERP.Application.Facturacion;
using EstacionERP.Application.Productos;
using EstacionERP.Application.Seguridad;
using EstacionERP.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios (casos de uso) de la capa Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISesionActual, SesionActual>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IFacturacionService, FacturacionService>();
        services.AddScoped<IConfiguracionFiscalService, ConfiguracionFiscalService>();
        return services;
    }
}
