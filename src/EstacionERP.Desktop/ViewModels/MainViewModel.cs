using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>
/// ViewModel de la ventana principal: maneja la navegación del menú lateral.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;

    [ObservableProperty]
    private object? _vistaActual;

    [ObservableProperty]
    private string _titulo = "Inicio";

    public MainViewModel(IServiceProvider sp)
    {
        _sp = sp;
        Navegar("Inicio");
    }

    [RelayCommand]
    private void Navegar(string destino)
    {
        (Titulo, VistaActual) = destino switch
        {
            "Inicio" => ("Inicio", (object)_sp.GetRequiredService<InicioViewModel>()),
            "Clientes" => ("Clientes", _sp.GetRequiredService<ClientesViewModel>()),
            "Productos" => ("Productos", new ProximamenteViewModel("Productos",
                "Alta de artículos, servicios y combustibles por unidad de negocio, con listas de precios.")),
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
            "Facturacion" => ("Facturación ARCA", new ProximamenteViewModel("Facturación ARCA",
                "Emisión de comprobantes A/B/C con CAE mediante WSAA + WSFEv1.")),
            _ => (Titulo, VistaActual)
        };
    }
}
