using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Common;
using EstacionERP.Application.Productos;
using EstacionERP.Application.Seguridad;
using EstacionERP.Desktop.Comun;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

public partial class ProductosViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ISesionActual _sesion;

    public ObservableCollection<ProductoResumen> Productos { get; } = new();

    /// <summary>Unidades para el filtro (incluye "Todas" con valor 0).</summary>
    public ObservableCollection<Opcion<int>> FiltroUnidades { get; } = new();

    /// <summary>Unidades para el formulario (solo las permitidas).</summary>
    public ObservableCollection<Opcion<int>> Unidades { get; } = new();

    public IReadOnlyList<Opcion<TipoProducto>> TiposProducto => Opciones.TiposProducto;
    public IReadOnlyList<Opcion<UnidadMedida>> UnidadesMedida => Opciones.UnidadesMedida;
    public IReadOnlyList<Opcion<AlicuotaIva>> Alicuotas => Opciones.Alicuotas;

    public bool PuedeEditar => _sesion.Puede(Permiso.EditarProductos);
    public bool PuedeModificarPrecios => _sesion.Puede(Permiso.ModificarPrecios);

    [ObservableProperty] private string? _busqueda;
    [ObservableProperty] private int _filtroUnidadId;

    /// <summary>null = todas las unidades.</summary>
    private int? UnidadFiltrada => FiltroUnidadId == 0 ? null : FiltroUnidadId;
    [ObservableProperty] private bool _incluirInactivos;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditarCommand))]
    private ProductoResumen? _seleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorVisible), nameof(PanelLateralVisible))]
    private ProductoEditorViewModel? _editor;

    // Panel de actualización masiva de precios.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanelLateralVisible))]
    private bool _aumentoVisible;
    [ObservableProperty] private decimal _aumentoPorcentaje;
    [ObservableProperty] private string? _aumentoRubro;
    [ObservableProperty] private string? _aumentoMarca;
    [ObservableProperty] private bool _aumentoTambienCosto = true;

    [ObservableProperty] private string? _estado;

    public bool EditorVisible => Editor is not null;
    public bool PanelLateralVisible => EditorVisible || AumentoVisible;

    public ProductosViewModel(IServiceScopeFactory scopes, ISesionActual sesion)
    {
        _scopes = scopes;
        _sesion = sesion;
    }

    public async Task CargarAsync()
    {
        using (var scope = _scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IEstacionDbContext>();
            var unidades = await db.UnidadesNegocio.AsNoTracking().Where(u => u.Activa).OrderBy(u => u.Id).ToListAsync();

            FiltroUnidades.Clear();
            Unidades.Clear();
            FiltroUnidades.Add(new Opcion<int>(0, "Todas las unidades"));
            foreach (var u in unidades.Where(u => _sesion.TieneAcceso(u.Id)))
            {
                FiltroUnidades.Add(new Opcion<int>(u.Id, u.Nombre));
                Unidades.Add(new Opcion<int>(u.Id, u.Nombre));
            }
        }
        await BuscarAsync();
    }

    partial void OnFiltroUnidadIdChanged(int value) => _ = BuscarAsync();
    partial void OnIncluirInactivosChanged(bool value) => _ = BuscarAsync();

    [RelayCommand]
    private async Task BuscarAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IProductoService>();
        var lista = await servicio.BuscarAsync(Busqueda, UnidadFiltrada, IncluirInactivos);

        Productos.Clear();
        foreach (var p in lista) Productos.Add(p);
        Estado = lista.Count == 1 ? "1 producto" : $"{lista.Count} productos";
    }

    [RelayCommand]
    private void Nuevo()
    {
        if (!PuedeEditar) return;
        AumentoVisible = false;
        var unidad = UnidadFiltrada ?? Unidades.FirstOrDefault()?.Valor ?? 0;
        Editor = new ProductoEditorViewModel(new ProductoDatos { UnidadNegocioId = unidad }, PuedeModificarPrecios);
    }

    private bool HaySeleccion() => Seleccionado is not null;

    [RelayCommand(CanExecute = nameof(HaySeleccion))]
    private async Task EditarAsync()
    {
        if (Seleccionado is null || !PuedeEditar) return;
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IProductoService>();
        var datos = await servicio.ObtenerAsync(Seleccionado.Id);
        if (datos is null)
        {
            MessageBox.Show("El producto ya no existe o no tenés acceso.", "Productos");
            return;
        }
        AumentoVisible = false;
        Editor = new ProductoEditorViewModel(datos, PuedeModificarPrecios);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Editor is null) return;
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IProductoService>();
        var r = await servicio.GuardarAsync(Editor.ADatos());
        if (!r.Exito)
        {
            Editor.Errores = string.Join(Environment.NewLine, r.Errores.Select(e => "• " + e));
            return;
        }
        var eraNuevo = Editor.EsNuevo;
        Editor = null;
        await BuscarAsync();
        Seleccionado = Productos.FirstOrDefault(p => p.Id == r.Valor);
        Estado = eraNuevo ? "Producto creado correctamente." : "Producto actualizado correctamente.";
    }

    [RelayCommand]
    private void Cancelar()
    {
        Editor = null;
        AumentoVisible = false;
    }

    // ----- Actualización masiva de precios -----

    [RelayCommand]
    private void MostrarAumento()
    {
        if (!PuedeModificarPrecios) return;
        Editor = null;
        AumentoPorcentaje = 0;
        AumentoVisible = true;
    }

    [RelayCommand]
    private async Task AplicarAumentoAsync()
    {
        var filtro = new FiltroAumento { UnidadNegocioId = UnidadFiltrada, Rubro = AumentoRubro, Marca = AumentoMarca };

        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IProductoService>();

        var cantidad = await servicio.ContarParaAumentoAsync(filtro);
        if (cantidad == 0)
        {
            MessageBox.Show("No hay productos activos que coincidan con el filtro.", "Actualizar precios");
            return;
        }

        var unidad = FiltroUnidades.FirstOrDefault(u => u.Valor == FiltroUnidadId)?.Texto ?? "Todas las unidades";
        var texto = $"Se va a aplicar un {AumentoPorcentaje:N2} % a {cantidad} producto(s).\n\n" +
                    $"Unidad: {unidad}\nRubro: {(string.IsNullOrWhiteSpace(AumentoRubro) ? "todos" : AumentoRubro)}\n" +
                    $"Marca: {(string.IsNullOrWhiteSpace(AumentoMarca) ? "todas" : AumentoMarca)}\n" +
                    $"Costo: {(AumentoTambienCosto ? "también se actualiza" : "no se modifica")}\n\n¿Confirmás?";
        if (MessageBox.Show(texto, "Actualizar precios", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var r = await servicio.AumentarPreciosAsync(filtro, AumentoPorcentaje, AumentoTambienCosto);
        if (!r.Exito)
        {
            MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "Actualizar precios",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AumentoVisible = false;
        await BuscarAsync();
        Estado = $"Precios actualizados en {r.Valor} producto(s).";
    }
}
