using CommunityToolkit.Mvvm.ComponentModel;
using EstacionERP.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

public partial class CambiarPasswordViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ISesionActual _sesion;

    [ObservableProperty] private string? _error;

    public string NombreUsuario => _sesion.Usuario?.NombreCompleto ?? string.Empty;

    public CambiarPasswordViewModel(IServiceScopeFactory scopes, ISesionActual sesion)
    {
        _scopes = scopes;
        _sesion = sesion;
    }

    public async Task<bool> CambiarAsync(string actual, string nueva, string confirmacion)
    {
        Error = null;
        using var scope = _scopes.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var r = await auth.CambiarPasswordAsync(_sesion.Usuario!.Id, actual, nueva, confirmacion);
        if (!r.Exito)
        {
            Error = r.Errores[0];
            return false;
        }
        _sesion.MarcarPasswordCambiada();
        return true;
    }
}
