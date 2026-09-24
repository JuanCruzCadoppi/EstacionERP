using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EstacionERP.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

public record TarjetaUnidad(string Nombre, string Codigo);

public partial class InicioViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;

    public ObservableCollection<TarjetaUnidad> Unidades { get; } = new();

    [ObservableProperty]
    private int _cantidadClientes;

    [ObservableProperty]
    private string _fecha = DateTime.Now.ToString("dddd d 'de' MMMM 'de' yyyy");

    public InicioViewModel(IServiceScopeFactory scopes) => _scopes = scopes;

    public async Task CargarAsync()
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEstacionDbContext>();

        var unidades = await db.UnidadesNegocio.AsNoTracking()
            .Where(u => u.Activa).OrderBy(u => u.Id).ToListAsync();

        Unidades.Clear();
        foreach (var u in unidades)
            Unidades.Add(new TarjetaUnidad(u.Nombre, u.Codigo));

        CantidadClientes = await db.Clientes.CountAsync(c => c.Activo);
    }
}
