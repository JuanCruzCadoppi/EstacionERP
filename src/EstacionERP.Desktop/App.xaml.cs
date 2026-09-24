using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using EstacionERP.Application;
using EstacionERP.Desktop.ViewModels;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Formato argentino para números y fechas (1.234,56 y dd/MM/yyyy).
        var cultura = new CultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Error inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
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

                services.AddSingleton<MainViewModel>();
                services.AddTransient<InicioViewModel>();
                services.AddTransient<ClientesViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        Servicios = _host.Services;

        if (!await PrepararBaseDeDatosAsync())
        {
            Shutdown(1);
            return;
        }

        var ventana = Servicios.GetRequiredService<MainWindow>();
        MainWindow = ventana;
        ventana.Show();
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
                $"en {Path.Combine(AppContext.BaseDirectory, "appsettings.json")} sea correcta.\n\n" +
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
