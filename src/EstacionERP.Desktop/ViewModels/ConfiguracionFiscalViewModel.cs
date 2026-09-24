using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Common;
using EstacionERP.Application.Configuracion;
using EstacionERP.Desktop.Comun;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace EstacionERP.Desktop.ViewModels;

public partial class ConfiguracionFiscalViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;

    public IReadOnlyList<Opcion<CondicionIva>> CondicionesEmisor => Opciones.CondicionesEmisor;
    public IReadOnlyList<Opcion<EntornoArca>> Entornos => Opciones.Entornos;
    public ObservableCollection<Opcion<int>> Unidades { get; } = new();
    public ObservableCollection<PuntoVentaFila> PuntosVenta { get; } = new();
    public IReadOnlyList<Opcion<FormatoImpresion>> Formatos => Opciones.FormatosImpresion;
    /// <summary>Impresoras instaladas en esta PC. La primera opción vacía = predeterminada de Windows.</summary>
    public ObservableCollection<string> Impresoras { get; } = new();

    // Datos de la empresa
    [ObservableProperty] private string _cuit = string.Empty;
    [ObservableProperty] private string _razonSocial = string.Empty;
    [ObservableProperty] private string? _nombreFantasia;
    [ObservableProperty] private CondicionIva _condicionIva = CondicionIva.ResponsableInscripto;
    [ObservableProperty] private string? _domicilio;
    [ObservableProperty] private string? _localidad;
    [ObservableProperty] private string? _ingresosBrutos;
    [ObservableProperty] private DateTime? _inicioActividades;
    [ObservableProperty] private EntornoArca _entorno = EntornoArca.Homologacion;
    [ObservableProperty] private string? _mensajeDatos;

    // Certificado
    [ObservableProperty] private string _alias = "estacionerp";
    [ObservableProperty] private string _estadoCertificado = "Sin certificado.";
    [ObservableProperty] private string? _resultadoPrueba;
    [ObservableProperty] private bool _probando;

    // Punto de venta en edición
    [ObservableProperty] private PuntoVentaFila? _puntoSeleccionado;
    [ObservableProperty] private int _pvId;
    [ObservableProperty] private int _pvNumero = 1;
    [ObservableProperty] private string _pvDescripcion = string.Empty;
    [ObservableProperty] private int _pvUnidadId = 1;
    [ObservableProperty] private bool _pvActivo = true;
    [ObservableProperty] private FormatoImpresion _pvFormato = FormatoImpresion.A4;
    [ObservableProperty] private string? _pvImpresora;
    [ObservableProperty] private bool _pvImprimirAlEmitir = true;
    [ObservableProperty] private string? _mensajePv;

    public ConfiguracionFiscalViewModel(IServiceScopeFactory scopes) => _scopes = scopes;

    public async Task CargarAsync()
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEstacionDbContext>();
        var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();

        Unidades.Clear();
        foreach (var u in await db.UnidadesNegocio.AsNoTracking().OrderBy(u => u.Id).ToListAsync())
            Unidades.Add(new Opcion<int>(u.Id, u.Nombre));

        Impresoras.Clear();
        Impresoras.Add(string.Empty);
        foreach (var nombre in Impresion.ImpresoraWindows.Instaladas()) Impresoras.Add(nombre);

        var d = await servicio.ObtenerAsync();
        Cuit = d.Cuit;
        RazonSocial = d.RazonSocial;
        NombreFantasia = d.NombreFantasia;
        CondicionIva = d.CondicionIva;
        Domicilio = d.Domicilio;
        Localidad = d.Localidad ?? "Laguna Larga";
        IngresosBrutos = d.IngresosBrutos;
        InicioActividades = d.InicioActividades;
        Entorno = d.Entorno;

        await RefrescarCertificadoAsync(servicio);
        await RefrescarPuntosAsync(servicio);
    }

    private async Task RefrescarCertificadoAsync(IConfiguracionFiscalService servicio)
    {
        var e = await servicio.EstadoCertificadoAsync();
        if (e.Alias is not null) Alias = e.Alias;
        EstadoCertificado = e.TieneCertificado
            ? $"Certificado cargado (alias \"{e.Alias}\"), vence el {e.Vence!.Value.ToLocalTime():dd/MM/yyyy}."
            : "Sin certificado.";
        if (e.TieneSolicitudPendiente)
            EstadoCertificado += " Hay una solicitud generada esperando el certificado de ARCA.";
    }

    private async Task RefrescarPuntosAsync(IConfiguracionFiscalService servicio)
    {
        PuntosVenta.Clear();
        foreach (var p in await servicio.ListarPuntosVentaAsync()) PuntosVenta.Add(p);
    }

    [RelayCommand]
    private async Task GuardarDatosAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();
        var r = await servicio.GuardarAsync(new ConfiguracionFiscalDatos
        {
            Cuit = Cuit, RazonSocial = RazonSocial, NombreFantasia = NombreFantasia, CondicionIva = CondicionIva,
            Domicilio = Domicilio, Localidad = Localidad, IngresosBrutos = IngresosBrutos,
            InicioActividades = InicioActividades, Entorno = Entorno
        });
        MensajeDatos = r.Exito ? "Datos guardados." : string.Join(Environment.NewLine, r.Errores);
        if (r.Exito) await RefrescarCertificadoAsync(servicio);
    }

    [RelayCommand]
    private async Task GenerarSolicitudAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();

        var estado = await servicio.EstadoCertificadoAsync();
        if (estado.TieneSolicitudPendiente &&
            MessageBox.Show("Ya hay una solicitud generada. Si generás otra, el certificado de la anterior no se va a poder importar. ¿Continuar?",
                "Certificado", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var r = await servicio.GenerarSolicitudCertificadoAsync(Alias);
        if (!r.Exito)
        {
            MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "Certificado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialogo = new SaveFileDialog
        {
            Title = "Guardar solicitud de certificado",
            FileName = $"{Alias}.csr",
            Filter = "Solicitud de certificado (*.csr)|*.csr"
        };
        if (dialogo.ShowDialog() == true)
            await File.WriteAllTextAsync(dialogo.FileName, r.Valor);

        Clipboard.SetText(r.Valor!);
        MessageBox.Show(
            "Solicitud generada y copiada al portapapeles.\n\n" +
            "Ahora, en ARCA (servicio WSASS para homologación):\n" +
            "1. Nuevo Certificado → alias \"" + Alias + "\" → pegá el texto de la solicitud.\n" +
            "2. Copiá el certificado que te devuelve y guardalo como archivo .crt.\n" +
            "3. Creá la autorización para el servicio \"wsfe\".\n" +
            "4. Volvé acá y usá \"Importar certificado\".",
            "Certificado", MessageBoxButton.OK, MessageBoxImage.Information);
        await RefrescarCertificadoAsync(servicio);
    }

    [RelayCommand]
    private async Task ImportarCertificadoAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Title = "Elegí el certificado que descargaste de ARCA",
            Filter = "Certificado (*.crt;*.pem;*.cer)|*.crt;*.pem;*.cer|Todos los archivos (*.*)|*.*"
        };
        if (dialogo.ShowDialog() != true) return;

        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();
        var r = await servicio.ImportarCertificadoAsync(await File.ReadAllTextAsync(dialogo.FileName));
        if (r.Exito)
            MessageBox.Show($"Certificado importado. Vence el {r.Valor:dd/MM/yyyy}.\n\nAhora probá la conexión.", "Certificado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "Certificado", MessageBoxButton.OK, MessageBoxImage.Warning);
        await RefrescarCertificadoAsync(servicio);
    }

    [RelayCommand]
    private async Task ProbarConexionAsync()
    {
        Probando = true;
        ResultadoPrueba = "Probando conexión con ARCA...";
        try
        {
            using var scope = _scopes.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();
            ResultadoPrueba = string.Join(Environment.NewLine, await servicio.ProbarConexionAsync());
        }
        finally
        {
            Probando = false;
        }
    }

    partial void OnPuntoSeleccionadoChanged(PuntoVentaFila? value)
    {
        if (value is null) return;
        PvId = value.Id;
        PvNumero = value.Numero;
        PvDescripcion = value.Descripcion;
        PvUnidadId = value.UnidadNegocioId;
        PvActivo = value.Activo;
        PvFormato = value.FormatoImpresion;
        PvImpresora = value.Impresora ?? string.Empty;
        PvImprimirAlEmitir = value.ImprimirAlEmitir;
        MensajePv = null;
    }

    [RelayCommand]
    private void NuevoPunto()
    {
        PuntoSeleccionado = null;
        PvId = 0;
        PvNumero = PuntosVenta.Count == 0 ? 1 : PuntosVenta.Max(p => p.Numero) + 1;
        PvDescripcion = string.Empty;
        PvUnidadId = Unidades.FirstOrDefault()?.Valor ?? 1;
        PvActivo = true;
        PvFormato = FormatoImpresion.A4;
        PvImpresora = string.Empty;
        PvImprimirAlEmitir = true;
        MensajePv = null;
    }

    [RelayCommand]
    private async Task GuardarPuntoAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IConfiguracionFiscalService>();
        var r = await servicio.GuardarPuntoVentaAsync(new PuntoVentaFila(PvId, PvNumero, PvDescripcion, PvUnidadId, string.Empty, PvActivo,
            PvFormato, string.IsNullOrWhiteSpace(PvImpresora) ? null : PvImpresora, PvImprimirAlEmitir));
        MensajePv = r.Exito ? "Punto de venta guardado." : string.Join(Environment.NewLine, r.Errores);
        if (r.Exito)
        {
            PvId = r.Valor;
            await RefrescarPuntosAsync(servicio);
        }
    }
}
