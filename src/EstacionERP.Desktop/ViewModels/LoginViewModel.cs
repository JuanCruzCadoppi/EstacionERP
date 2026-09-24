using CommunityToolkit.Mvvm.ComponentModel;
using EstacionERP.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ISesionActual _sesion;

    [ObservableProperty] private string _nombreUsuario = string.Empty;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public LoginViewModel(IServiceScopeFactory scopes, ISesionActual sesion)
    {
        _scopes = scopes;
        _sesion = sesion;
    }

    /// <summary>La contraseña llega desde el PasswordBox (WPF no permite enlazarla por seguridad).</summary>
    public async Task<bool> IngresarAsync(string password)
    {
        if (Ocupado) return false;
        Ocupado = true;
        Error = null;
        try
        {
            using var scope = _scopes.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var r = await auth.IngresarAsync(NombreUsuario, password);
            if (!r.Exito)
            {
                Error = r.Errores[0];
                return false;
            }
            _sesion.Iniciar(r.Valor!);
            return true;
        }
        finally
        {
            Ocupado = false;
        }
    }
}
