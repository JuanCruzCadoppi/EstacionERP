using System.Windows;
using EstacionERP.Desktop.ViewModels;
using EstacionERP.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EstacionERP.Desktop;

public partial class MainWindow : Window
{
    /// <summary>true si la ventana se cierra para volver al login (y no para salir del programa).</summary>
    public bool CerrandoSesion { get; private set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CerrarSesionSolicitado += (_, _) =>
        {
            CerrandoSesion = true;
            Close();
        };
    }

    private void AlCambiarPassword(object sender, RoutedEventArgs e)
    {
        var vm = App.Servicios.GetRequiredService<CambiarPasswordViewModel>();
        new CambiarPasswordWindow(vm, obligatorio: false) { Owner = this }.ShowDialog();
    }
}
