using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Clientes;
using EstacionERP.Application.Seguridad;
using EstacionERP.Desktop.Comun;
using EstacionERP.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>
/// Pantalla de clientes: grilla con búsqueda + panel lateral de edición.
/// Cada operación abre su propio "scope" para usar un DbContext nuevo y corto.
/// </summary>
public partial class ClientesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ISesionActual _sesion;

    public bool PuedeCambiarEstado => _sesion.Puede(Permiso.DesactivarClientes);

    public ObservableCollection<ClienteResumen> Clientes { get; } = new();

    public IReadOnlyList<Opcion<TipoDocumento>> TiposDocumento => Opciones.TiposDocumento;
    public IReadOnlyList<Opcion<CondicionIva>> CondicionesIva => Opciones.CondicionesIva;

    [ObservableProperty] private string? _busqueda;
    [ObservableProperty] private bool _incluirInactivos;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditarCommand))]
    [NotifyCanExecuteChangedFor(nameof(CambiarEstadoCommand))]
    private ClienteResumen? _seleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorVisible))]
    private ClienteEditorViewModel? _editor;

    [ObservableProperty] private string? _estado;

    public bool EditorVisible => Editor is not null;

    public ClientesViewModel(IServiceScopeFactory scopes, ISesionActual sesion)
    {
        _scopes = scopes;
        _sesion = sesion;
    }

    public Task CargarAsync() => BuscarAsync();

    partial void OnIncluirInactivosChanged(bool value) => _ = BuscarAsync();

    [RelayCommand]
    private async Task BuscarAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IClienteService>();

        var lista = await servicio.BuscarAsync(Busqueda, IncluirInactivos);
        Clientes.Clear();
        foreach (var c in lista)
            Clientes.Add(c);

        Estado = lista.Count == 1 ? "1 cliente" : $"{lista.Count} clientes";
    }

    [RelayCommand]
    private void Nuevo() => Editor = new ClienteEditorViewModel(new ClienteDatos());

    private bool HaySeleccion() => Seleccionado is not null;

    [RelayCommand(CanExecute = nameof(HaySeleccion))]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) return;

        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IClienteService>();

        var datos = await servicio.ObtenerAsync(Seleccionado.Id);
        if (datos is null)
        {
            MessageBox.Show("El cliente ya no existe.", "Clientes");
            await BuscarAsync();
            return;
        }

        Editor = new ClienteEditorViewModel(datos);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Editor is null) return;

        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IClienteService>();

        var resultado = await servicio.GuardarAsync(Editor.ADatos());
        if (!resultado.Exito)
        {
            Editor.Errores = string.Join(Environment.NewLine, resultado.Errores.Select(e => "• " + e));
            return;
        }

        var eraNuevo = Editor.EsNuevo;
        Editor = null;
        await BuscarAsync();
        Seleccionado = Clientes.FirstOrDefault(c => c.Id == resultado.Valor);
        Estado = eraNuevo ? "Cliente creado correctamente." : "Cliente actualizado correctamente.";
    }

    [RelayCommand]
    private void Cancelar() => Editor = null;

    [RelayCommand(CanExecute = nameof(HaySeleccion))]
    private async Task CambiarEstadoAsync()
    {
        if (Seleccionado is null) return;

        var activar = !Seleccionado.Activo;
        var accion = activar ? "activar" : "desactivar";
        if (MessageBox.Show($"¿Querés {accion} a {Seleccionado.RazonSocial}?", "Clientes",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IClienteService>();

        var r = await servicio.CambiarEstadoAsync(Seleccionado.Id, activar);
        if (!r.Exito)
        {
            MessageBox.Show(string.Join(Environment.NewLine, r.Errores), "Clientes",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await BuscarAsync();
    }
}
