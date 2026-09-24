using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstacionERP.Application.Common;
using EstacionERP.Application.Usuarios;
using EstacionERP.Desktop.Comun;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>Checkbox de unidad de negocio en el formulario de usuario.</summary>
public partial class UnidadSeleccionable : ObservableObject
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    [ObservableProperty] private bool _seleccionada;
}

public partial class UsuarioEditorViewModel : ObservableObject
{
    public int Id { get; }
    public bool EsNuevo => Id == 0;
    public string TituloFormulario => EsNuevo ? "Nuevo usuario" : "Editar usuario";
    public string EtiquetaPassword => EsNuevo
        ? "Contraseña inicial *"
        : "Nueva contraseña (dejar vacío para no cambiarla)";

    [ObservableProperty] private string _nombreUsuario;
    [ObservableProperty] private string _nombreCompleto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnidadesHabilitadas))]
    private Rol _rol;

    [ObservableProperty] private bool _activo;
    [ObservableProperty] private string? _nuevaPassword;
    [ObservableProperty] private string? _errores;

    public ObservableCollection<UnidadSeleccionable> Unidades { get; }

    /// <summary>El administrador accede a todas: no hace falta marcar.</summary>
    public bool UnidadesHabilitadas => Rol != Rol.Administrador;

    public UsuarioEditorViewModel(UsuarioDatos d, IEnumerable<(int Id, string Nombre)> unidades)
    {
        Id = d.Id;
        _nombreUsuario = d.NombreUsuario;
        _nombreCompleto = d.NombreCompleto;
        _rol = d.Rol;
        _activo = d.Activo;
        Unidades = new ObservableCollection<UnidadSeleccionable>(unidades.Select(u => new UnidadSeleccionable
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Seleccionada = d.UnidadesNegocioIds.Contains(u.Id)
        }));
    }

    public UsuarioDatos ADatos() => new()
    {
        Id = Id,
        NombreUsuario = NombreUsuario,
        NombreCompleto = NombreCompleto,
        Rol = Rol,
        Activo = Activo,
        NuevaPassword = NuevaPassword,
        UnidadesNegocioIds = Unidades.Where(u => u.Seleccionada).Select(u => u.Id).ToList()
    };
}

public partial class UsuariosViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private List<(int Id, string Nombre)> _unidades = new();

    public ObservableCollection<UsuarioResumen> Usuarios { get; } = new();
    public IReadOnlyList<Opcion<Rol>> Roles => Opciones.Roles;

    [ObservableProperty] private bool _incluirInactivos;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditarCommand))]
    private UsuarioResumen? _seleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorVisible))]
    private UsuarioEditorViewModel? _editor;

    [ObservableProperty] private string? _estado;

    public bool EditorVisible => Editor is not null;

    public UsuariosViewModel(IServiceScopeFactory scopes) => _scopes = scopes;

    public async Task CargarAsync()
    {
        using (var scope = _scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IEstacionDbContext>();
            _unidades = (await db.UnidadesNegocio.AsNoTracking().Where(u => u.Activa).OrderBy(u => u.Id).ToListAsync())
                .Select(u => (u.Id, u.Nombre)).ToList();
        }
        await ListarAsync();
    }

    partial void OnIncluirInactivosChanged(bool value) => _ = ListarAsync();

    [RelayCommand]
    private async Task ListarAsync()
    {
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        var lista = await servicio.ListarAsync(IncluirInactivos);
        Usuarios.Clear();
        foreach (var u in lista) Usuarios.Add(u);
        Estado = lista.Count == 1 ? "1 usuario" : $"{lista.Count} usuarios";
    }

    [RelayCommand]
    private void Nuevo() => Editor = new UsuarioEditorViewModel(new UsuarioDatos(), _unidades);

    private bool HaySeleccion() => Seleccionado is not null;

    [RelayCommand(CanExecute = nameof(HaySeleccion))]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) return;
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        var datos = await servicio.ObtenerAsync(Seleccionado.Id);
        if (datos is not null)
            Editor = new UsuarioEditorViewModel(datos, _unidades);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Editor is null) return;
        using var scope = _scopes.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        var r = await servicio.GuardarAsync(Editor.ADatos());
        if (!r.Exito)
        {
            Editor.Errores = string.Join(Environment.NewLine, r.Errores.Select(e => "• " + e));
            return;
        }
        var eraNuevo = Editor.EsNuevo;
        Editor = null;
        await ListarAsync();
        Seleccionado = Usuarios.FirstOrDefault(u => u.Id == r.Valor);
        Estado = eraNuevo
            ? "Usuario creado. Va a tener que cambiar la contraseña en su primer ingreso."
            : "Usuario actualizado correctamente.";
    }

    [RelayCommand]
    private void Cancelar() => Editor = null;
}
