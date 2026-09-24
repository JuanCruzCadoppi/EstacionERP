using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using EstacionERP.Application;
using EstacionERP.Application.Seguridad;
using EstacionERP.Desktop.ViewModels;
using EstacionERP.Desktop.Views;
using EstacionERP.Infrastructure;
using EstacionERP.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EstacionERP.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    public static IServiceProvider Servicios { get; private set; } = null!;

    /// <summary>
    /// Registra el idioma es-AR para <paramref name="tipo"/> solo si todavía no tiene metadata propia:
    /// evita el ArgumentException "El valor PropertyMetadata ya está registrado" cuando el proceso anterior
    /// de Visual Studio quedó vivo o cuando WPF ya aplicó el idioma por su cuenta.
    /// </summary>
    private static void AplicarIdiomaSiHaceFalta(DependencyProperty propiedad, Type tipo, CultureInfo cultura)
    {
        try
        {
            propiedad.OverrideMetadata(tipo, new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));
        }
        catch (ArgumentException)
        {
            // Ya estaba registrado (proceso anterior vivo, hot reload, etc.). No hace falta hacer nada más:
            // el idioma efectivo va a ser el que haya quedado, que en la práctica también es es-AR.
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // La app decide cuándo cerrarse (entre el login y la ventana principal no hay ventanas abiertas).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Formato argentino para números y fechas (1.234,56 y dd/MM/yyyy).
        var cultura = new CultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        // OverrideMetadata solo se puede llamar una vez por tipo en todo el proceso. Si ya está registrado
        // (por ejemplo porque Visual Studio dejó vivo un proceso anterior, o WPF ya lo hizo por su cuenta),
        // OverrideMetadata tira ArgumentException. AplicarIdiomaSiHaceFalta lo prueba antes de llamarlo.
        AplicarIdiomaSiHaceFalta(FrameworkElement.LanguageProperty, typeof(FrameworkElement), cultura);
        AplicarIdiomaSiHaceFalta(FrameworkContentElement.LanguageProperty, typeof(FrameworkContentElement), cultura);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.GetBaseException().Message, "Error inesperado",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                c.SetBasePath(AppContext.BaseDirectory);
                c.AddJsonFile("appsettings.json", optional: false);
                // Configuración de cada PC (contraseñas, etc.). No se sube a Git.
                c.AddJsonFile("appsettings.Local.json", optional: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cs = ctx.Configuration.GetConnectionString("Estacion")
                         ?? throw new InvalidOperationException("Falta la cadena de conexión 'Estacion' en appsettings.json");

                services.AddApplication();
                services.AddInfrastructure(cs);

                services.AddTransient<LoginViewModel>();
                services.AddTransient<CambiarPasswordViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<InicioViewModel>();
                services.AddTransient<ClientesViewModel>();
                services.AddTransient<ProductosViewModel>();
                services.AddTransient<UsuariosViewModel>();
                services.AddTransient<FacturacionViewModel>();
                services.AddTransient<ConfiguracionFiscalViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        Servicios = _host.Services;

        if (!await PrepararBaseDeDatosAsync())
        {
            Shutdown(1);
            return;
        }

        MostrarLogin();
    }

    /// <summary>
    /// Muestra el login (y el cambio de contraseña si corresponde). Si todo sale bien abre la ventana principal.
    /// </summary>
    public void MostrarLogin()
    {
        var sesion = Servicios.GetRequiredService<ISesionActual>();

        var login = new LoginWindow(Servicios.GetRequiredService<LoginViewModel>());
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        if (sesion.Usuario!.DebeCambiarPassword)
        {
            var cambio = new CambiarPasswordWindow(Servicios.GetRequiredService<CambiarPasswordViewModel>(), obligatorio: true);
            if (cambio.ShowDialog() != true)
            {
                sesion.Cerrar();
                Shutdown();
                return;
            }
        }

        var principal = Servicios.GetRequiredService<MainWindow>();
        MainWindow = principal;
        principal.Closed += (_, _) =>
        {
            if (principal.CerrandoSesion)
            {
                sesion.Cerrar();
                MostrarLogin();
            }
            else
            {
                Shutdown();
            }
        };
        principal.Show();
    }

    /// <summary>
    /// Crea la base o aplica las migraciones pendientes al iniciar.
    /// </summary>
    private static async Task<bool> PrepararBaseDeDatosAsync()
    {
        try
        {
            using var scope = Servicios.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EstacionDbContext>();
            await db.Database.MigrateAsync();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo conectar con la base de datos.\n\n" +
                "Revisá que PostgreSQL esté instalado y en ejecución, y que la cadena de conexión " +
                $"en {Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json")} sea correcta.\n\n" +
                $"Detalle: {ex.GetBaseException().Message}",
                "Estación ERP", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
