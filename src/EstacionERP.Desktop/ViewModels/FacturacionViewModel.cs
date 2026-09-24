using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Clientes;
using EstacionERP.Application.Common;
using EstacionERP.Application.Configuracion;
using EstacionERP.Application.Facturacion;
using EstacionERP.Application.Impresion;
using EstacionERP.Desktop.Impresion;
using Microsoft.Win32;
using EstacionERP.Application.Productos;
using EstacionERP.Application.Seguridad;
using EstacionERP.Desktop.Comun;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>Renglón editable de la factura en pantalla.</summary>
public partial class ItemVentaViewModel : ObservableObject
{
    public int? ProductoId { get; init; }
    public string? Codigo { get; init; }
    [ObservableProperty] private string _descripcion = string.Empty;
    [ObservableProperty] private decimal _cantidad = 1;
    [ObservableProperty] private decimal _precioUnitario;
    [ObservableProperty] private AlicuotaIva _alicuotaIva = AlicuotaIva.Veintiuno;
    [ObservableProperty] private bool _esServicio;

    public decimal Total => Math.Round(Cantidad * PrecioUnitario, 2);

    public event EventHandler? Cambio;

    partial void OnCantidadChanged(decimal value) => Avisar();
    partial void OnPrecioUnitarioChanged(decimal value) => Avisar();
    partial void OnAlicuotaIvaChanged(AlicuotaIva value) => Avisar();
    partial void OnEsServicioChanged(bool value) => Avisar();

    private void Avisar()
    {
        OnPropertyChanged(nameof(Total));
        Cambio?.Invoke(this, EventArgs.Empty);
    }
}

public partial class FacturacionViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ISesionActual _sesion;
    private List<PuntoVentaFila> _todosLosPuntos = new();
    private TipoComprobante _tipoActual = TipoComprobante.FacturaB;

    public IReadOnlyList<Opcion<AlicuotaIva>> Alicuotas => Opciones.Alicuotas;
    public ObservableCollection<Opcion<int>> Unidades { get; } = new();
    public ObservableCollection<PuntoVentaFila> PuntosVenta { get; } = new();
    public ObservableCollection<ClienteResumen> Clientes { get; } = new();
    public ObservableCollection<ProductoResumen> ProductosEncontrados { get; } = new();
    public ObservableCollection<ItemVentaViewModel> Items { get; } = new();
    public ObservableCollection<ComprobanteResumen> Comprobantes { get; } = new();

    // ----- Nueva factura
    [ObservableProperty] private int _unidadId;
    [ObservableProperty] private PuntoVentaFila? _puntoVenta;
    [ObservableProperty] private string? _busquedaCliente;
    [ObservableProperty] private ClienteResumen? _cliente;
    [ObservableProperty] private string _tipoTexto = string.Empty;
    [ObservableProperty] private string? _busquedaProducto;
    [ObservableProperty] private ProductoResumen? _productoSeleccionado;
    [ObservableProperty] private ItemVentaViewModel? _itemSeleccionado;
    [ObservableProperty] private decimal _neto;
    [ObservableProperty] private decimal _iva;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private bool _discriminaIva = true;
    [ObservableProperty] private bool _emitiendo;
    [ObservableProperty] private string? _aviso;
    [ObservableProperty] private bool _modoPruebas;

    // ----- Comprobantes emitidos
    [ObservableProperty] private DateTime _desde = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _hasta = DateTime.Today;
    [ObservableProperty] private ComprobanteResumen? _comprobanteSeleccionado;
    [ObservableProperty] private string? _estadoLista;

    public FacturacionViewModel(IServiceScopeFactory scopes, ISesionActual sesion)
    {
        _scopes = scopes;
        _sesion = sesion;
        Items.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (ItemVentaViewModel i in e.NewItems) i.Cambio += (_, _) => Recalcular();
            Recalcular();
        };
    }

    public async Task CargarAsync()
    {
        using (var scope = _scopes.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<IEstacionDbContext>();

            var config = await db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync();
            ModoPruebas = config is null || config.Entorno == EntornoArca.Homologacion;
            Aviso = config is null
                ? "Todavía no se cargaron los datos fiscales. Pedile al administrador que complete ADMINISTRACIÓN → Configuración fiscal."
                : !config.TieneCertificado ? "Falta importar el certificado de ARCA (ADMINISTRACIÓN → Configuración fiscal)." : null;

            Unidades.Clear();
            foreach (var u in await db.UnidadesNegocio.AsNoTracking().Where(u => u.Activa).OrderBy(u => u.Id).ToListAsync())
                if (_sesion.TieneAcceso(u.Id)) Unidades.Add(new Opcion<int>(u.Id, u.Nombre));

            _todosLosPuntos = await sp.GetRequiredService<IConfiguracionFiscalService>().ListarPuntosVentaAsync(soloActivos: true);
        }

        if (UnidadId == 0 && Unidades.Count > 0) UnidadId = Unidades[0].Valor;
        else FiltrarPuntos();

        if (Cliente is null) await BuscarClienteAsync();
        await ListarAsync();
    }

    partial void OnUnidadIdChanged(int value) => FiltrarPuntos();

    private void FiltrarPuntos()
    {
        PuntosVenta.Clear();
        foreach (var p in _todosLosPuntos.Where(p => p.UnidadNegocioId == UnidadId)) PuntosVenta.Add(p);
        PuntoVenta = PuntosVenta.FirstOrDefault();
        ProductosEncontrados.Clear();
    }

    // ------------------------------------------------------------ Cliente

    [RelayCommand]
    private async Task BuscarClienteAsync()
    {
        using var scope = _scopes.CreateScope();
        var lista = await scope.ServiceProvider.GetRequiredService<IClienteService>().BuscarAsync(BusquedaCliente);
        Clientes.Clear();
        foreach (var c in lista.Take(100)) Clientes.Add(c);
        Cliente = string.IsNullOrWhiteSpace(BusquedaCliente)
            ? Clientes.FirstOrDefault(c => c.Id == Domain.Entidades.Cliente.ConsumidorFinalId) ?? Clientes.FirstOrDefault()
            : Clientes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ConsumidorFinalAsync()
    {
        BusquedaCliente = null;
        await BuscarClienteAsync();
    }

    partial void OnClienteChanged(ClienteResumen? value) => _ = ActualizarTipoAsync();

    private async Task ActualizarTipoAsync()
    {
        if (Cliente is null)
        {
            TipoTexto = string.Empty;
            return;
        }
        using var scope = _scopes.CreateScope();
        var tipo = await scope.ServiceProvider.GetRequiredService<IFacturacionService>().TipoParaClienteAsync(Cliente.Id);
        _tipoActual = tipo ?? TipoComprobante.FacturaB;
        TipoTexto = tipo is null ? "(faltan datos fiscales)" : tipo.Value.Texto();
        Recalcular();
    }

    // ------------------------------------------------------------ Productos e ítems

    [RelayCommand]
    private async Task BuscarProductoAsync()
    {
        using var scope = _scopes.CreateScope();
        var lista = await scope.ServiceProvider.GetRequiredService<IProductoService>().BuscarAsync(BusquedaProducto, UnidadId);
        ProductosEncontrados.Clear();
        foreach (var p in lista.Take(50)) ProductosEncontrados.Add(p);
        ProductoSeleccionado = ProductosEncontrados.FirstOrDefault();
        if (ProductosEncontrados.Count == 1) await AgregarProductoAsync();
    }

    [RelayCommand]
    private async Task AgregarProductoAsync()
    {
        if (ProductoSeleccionado is null) return;
        using var scope = _scopes.CreateScope();
        var p = await scope.ServiceProvider.GetRequiredService<IProductoService>().ObtenerAsync(ProductoSeleccionado.Id);
        if (p is null) return;

        var existente = Items.FirstOrDefault(i => i.ProductoId == p.Id);
        if (existente is not null)
        {
            existente.Cantidad += 1;
        }
        else
        {
            Items.Add(new ItemVentaViewModel
            {
                ProductoId = p.Id, Codigo = p.Codigo, Descripcion = p.Descripcion, PrecioUnitario = p.PrecioVenta,
                AlicuotaIva = p.AlicuotaIva, EsServicio = p.Tipo == TipoProducto.Servicio
            });
        }
        BusquedaProducto = null;
    }

    [RelayCommand]
    private void AgregarLineaLibre() =>
        Items.Add(new ItemVentaViewModel { Descripcion = "Varios", EsServicio = UnidadId == UnidadNegocio.LavaderoId });

    [RelayCommand]
    private void QuitarItem(ItemVentaViewModel? item)
    {
        if (item is not null) Items.Remove(item);
    }

    [RelayCommand]
    private void Limpiar()
    {
        Items.Clear();
        _ = ConsumidorFinalAsync();
    }

    private void Recalcular()
    {
        var r = CalculadoraComprobante.Calcular(
            Items.Select(i => new LineaCalculo(i.Cantidad, i.PrecioUnitario, i.AlicuotaIva, i.EsServicio)).ToList(), _tipoActual);
        Neto = r.Neto;
        Iva = r.Iva;
        Total = r.Total;
        DiscriminaIva = r.Iva > 0 || _tipoActual != TipoComprobante.FacturaC;
    }

    // ------------------------------------------------------------ Emisión

    [RelayCommand]
    private async Task EmitirAsync()
    {
        if (PuntoVenta is null)
        {
            MessageBox.Show("Esta unidad de negocio no tiene punto de venta. Cargalo en Configuración fiscal.", "Facturación",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Cliente is null || Items.Count == 0)
        {
            MessageBox.Show("Elegí el cliente y agregá al menos un ítem.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"¿Emitir {TipoTexto} a {Cliente.RazonSocial} por {Total:C2}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Emitiendo = true;
        try
        {
            using var scope = _scopes.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<IFacturacionService>().EmitirAsync(new SolicitudEmision
            {
                UnidadNegocioId = UnidadId,
                PuntoVentaId = PuntoVenta.Id,
                ClienteId = Cliente.Id,
                Items = Items.Select(i => new ItemEmision
                {
                    ProductoId = i.ProductoId, Codigo = i.Codigo, Descripcion = i.Descripcion, Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario, AlicuotaIva = i.AlicuotaIva, EsServicio = i.EsServicio
                }).ToList()
            });

            if (!r.Exito)
            {
                MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "No se pudo emitir", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var c = r.Valor!;
            switch (c.Estado)
            {
                case EstadoComprobante.Autorizado:
                    var impresion = await ImprimirSiCorrespondeAsync(c.Id);
                    MessageBox.Show($"{c.Tipo.Texto()} {c.NumeroCompleto} autorizada.\n\nCAE: {c.Cae}\nVence: {c.CaeVencimiento:dd/MM/yyyy}" +
                                    (string.IsNullOrWhiteSpace(c.Mensajes) ? "" : "\n\nObservaciones de ARCA:\n" + c.Mensajes) +
                                    (impresion is null ? "" : "\n\n" + impresion),
                        "Comprobante autorizado", MessageBoxButton.OK, MessageBoxImage.Information);
                    Limpiar();
                    break;
                case EstadoComprobante.Pendiente:
                    MessageBox.Show("El comprobante quedó PENDIENTE porque no se pudo confirmar con ARCA:\n\n" + c.Mensajes +
                                    "\n\nNo lo vuelvas a cargar: reintentalo desde la pestaña \"Comprobantes emitidos\".",
                        "Comprobante pendiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Limpiar();
                    break;
                default:
                    MessageBox.Show("ARCA rechazó el comprobante:\n\n" + c.Mensajes + "\n\nCorregí los datos y volvé a emitir.",
                        "Comprobante rechazado", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
            await ListarAsync();
        }
        finally
        {
            Emitiendo = false;
        }
    }

    // ------------------------------------------------------------ Impresión

    /// <summary>Imprime solo si el punto de venta lo tiene configurado. Devuelve el texto para el aviso.</summary>
    private async Task<string?> ImprimirSiCorrespondeAsync(int comprobanteId)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<IImpresionService>().PrepararAsync(comprobanteId);
            if (!r.Exito) return "No se pudo preparar la impresión: " + string.Join(" ", r.Errores);
            if (!r.Valor!.ImprimirAlEmitir) return null;
            var impresora = await ImpresoraWindows.ImprimirAsync(r.Valor.Pdf, r.Valor.Impresora, r.Valor.Comprobante.NombreArchivo);
            return $"Se envió a imprimir ({r.Valor.Formato.Texto()}) en: {impresora}";
        }
        catch (Exception ex)
        {
            return "ATENCIÓN: la factura es válida, pero no se pudo imprimir (" + ex.Message +
                   "). Podés reimprimirla desde \"Comprobantes emitidos\".";
        }
    }

    private async Task<DatosImpresion?> PrepararSeleccionadoAsync(FormatoImpresion? formato)
    {
        if (ComprobanteSeleccionado is not { Estado: EstadoComprobante.Autorizado } sel)
        {
            MessageBox.Show("Elegí un comprobante autorizado (con CAE).", "Imprimir", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        using var scope = _scopes.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<IImpresionService>().PrepararAsync(sel.Id, formato);
        if (r.Exito) return r.Valor;
        MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "Imprimir", MessageBoxButton.OK, MessageBoxImage.Warning);
        return null;
    }

    [RelayCommand]
    private async Task ImprimirAsync()
    {
        try
        {
            var d = await PrepararSeleccionadoAsync(null);
            if (d is null) return;
            var impresora = await ImpresoraWindows.ImprimirAsync(d.Pdf, d.Impresora, d.Comprobante.NombreArchivo);
            EstadoLista = $"{d.Comprobante.NumeroCompleto} enviado a {impresora} ({d.Formato.Texto()}).";
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo imprimir: " + ex.Message, "Imprimir", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>El PDF para ver o mandar por mail sale siempre en hoja A4.</summary>
    [RelayCommand]
    private async Task VerPdfAsync()
    {
        try
        {
            var d = await PrepararSeleccionadoAsync(FormatoImpresion.A4);
            if (d is not null) ImpresoraWindows.Abrir(d.Pdf, d.Comprobante.NombreArchivo);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo abrir el PDF: " + ex.Message, "PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task GuardarPdfAsync()
    {
        try
        {
            var d = await PrepararSeleccionadoAsync(FormatoImpresion.A4);
            if (d is null) return;
            var dialogo = new SaveFileDialog
            {
                Title = "Guardar comprobante en PDF",
                FileName = d.Comprobante.NombreArchivo,
                Filter = "PDF (*.pdf)|*.pdf"
            };
            if (dialogo.ShowDialog() == true)
            {
                await System.IO.File.WriteAllBytesAsync(dialogo.FileName, d.Pdf);
                EstadoLista = "PDF guardado en " + dialogo.FileName;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo guardar el PDF: " + ex.Message, "PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ------------------------------------------------------------ Comprobantes emitidos

    [RelayCommand]
    private async Task ListarAsync()
    {
        using var scope = _scopes.CreateScope();
        var lista = await scope.ServiceProvider.GetRequiredService<IFacturacionService>().ListarAsync(Desde, Hasta);
        Comprobantes.Clear();
        foreach (var c in lista) Comprobantes.Add(c);
        var pendientes = lista.Count(c => c.Estado == EstadoComprobante.Pendiente);
        EstadoLista = $"{lista.Count} comprobantes · {lista.Where(c => c.Estado == EstadoComprobante.Autorizado).Sum(c => c.Total):C2} autorizados" +
                      (pendientes > 0 ? $" · {pendientes} pendientes" : "");
    }

    [RelayCommand]
    private async Task ReintentarAsync()
    {
        if (ComprobanteSeleccionado is not { Estado: EstadoComprobante.Pendiente } sel)
        {
            MessageBox.Show("Elegí un comprobante en estado Pendiente.", "Reintentar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Emitiendo = true;
        try
        {
            using var scope = _scopes.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<IFacturacionService>().ReintentarAsync(sel.Id);
            MessageBox.Show(r.Exito ? $"{r.Valor!.NumeroCompleto}: {r.Valor.Estado.Texto()}\n{r.Valor.Mensajes}" : string.Join("\n", r.Errores),
                "Reintentar", MessageBoxButton.OK, MessageBoxImage.Information);
            await ListarAsync();
        }
        finally
        {
            Emitiendo = false;
        }
    }

    [RelayCommand]
    private async Task ReintentarPendientesAsync()
    {
        Emitiendo = true;
        try
        {
            using var scope = _scopes.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<IFacturacionService>().ReintentarPendientesAsync();
            MessageBox.Show($"Autorizados: {r.Autorizados}\nRechazados: {r.Rechazados}\nSiguen pendientes: {r.SiguenPendientes}",
                "Reintentar pendientes", MessageBoxButton.OK, MessageBoxImage.Information);
            await ListarAsync();
        }
        finally
        {
            Emitiendo = false;
        }
    }
}
