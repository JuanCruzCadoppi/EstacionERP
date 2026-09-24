using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>
/// ViewModel de la ventana principal: navegación del menú lateral y datos de la sesión.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly ISesionActual _sesion;

    [ObservableProperty] private object? _vistaActual;
    [ObservableProperty] private string _titulo = "Inicio";

    /// <summary>La ventana escucha este evento para cerrarse y volver al login.</summary>
    public event EventHandler? CerrarSesionSolicitado;

    public string UsuarioNombre => _sesion.Usuario?.NombreCompleto ?? string.Empty;
    public string UsuarioRol => _sesion.Usuario?.Rol.Texto() ?? string.Empty;

    // Visibilidad del menú según permisos.
    public bool VerPlaya => _sesion.TieneAcceso(UnidadNegocio.PlayaId);
    public bool VerRepuestos => _sesion.TieneAcceso(UnidadNegocio.RepuestosId);
    public bool VerLavadero => _sesion.TieneAcceso(UnidadNegocio.LavaderoId);
    public bool VerUsuarios => _sesion.Puede(Permiso.GestionarUsuarios);
    public bool VerFacturacion => _sesion.Puede(Permiso.Facturar);
    public bool VerConfiguracionFiscal => _sesion.Puede(Permiso.ConfigurarFacturacion);
    public bool VerAdministracion => VerUsuarios || VerConfiguracionFiscal;

    public MainViewModel(IServiceProvider sp, ISesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;
        Navegar("Inicio");
    }

    [RelayCommand]
    private void Navegar(string destino)
    {
        (Titulo, VistaActual) = destino switch
        {
            "Inicio" => ("Inicio", (object)_sp.GetRequiredService<InicioViewModel>()),
            "Clientes" => ("Clientes", _sp.GetRequiredService<ClientesViewModel>()),
            "Productos" => ("Productos", _sp.GetRequiredService<ProductosViewModel>()),
            "Usuarios" when VerUsuarios => ("Usuarios", _sp.GetRequiredService<UsuariosViewModel>()),
            "Playa" => ("Playa / Combustibles", new ProximamenteViewModel("Playa / Combustibles",
                "Tanques, surtidores, mangueras, turnos de playeros y lectura de aforadores.")),
            "Repuestos" => ("Repuestos", new ProximamenteViewModel("Repuestos",
                "Ventas de mostrador, presupuestos, stock y actualización de precios por proveedor.")),
            "Lavadero" => ("Lavadero", new ProximamenteViewModel("Lavadero",
                "Órdenes de lavado por patente, tablero de estados y facturación.")),
            "Caja" => ("Caja", new ProximamenteViewModel("Caja",
                "Apertura y cierre de caja por turno, medios de pago y arqueo.")),
            "CuentasCorrientes" => ("Cuentas corrientes", new ProximamenteViewModel("Cuentas corrientes",
                "Saldos por cliente y unidad de negocio, recibos e imputación de pagos.")),
            "Facturacion" when VerFacturacion => ("Facturación ARCA", _sp.GetRequiredService<FacturacionViewModel>()),
            "ConfiguracionFiscal" when VerConfiguracionFiscal => ("Configuración fiscal", _sp.GetRequiredService<ConfiguracionFiscalViewModel>()),
            _ => (Titulo, VistaActual)
        };
    }

    [RelayCommand]
    private void CerrarSesion() => CerrarSesionSolicitado?.Invoke(this, EventArgs.Empty);
}
