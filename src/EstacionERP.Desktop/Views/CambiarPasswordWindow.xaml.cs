using System.Windows;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class CambiarPasswordWindow : Window
{
    private readonly CambiarPasswordViewModel _vm;

    public CambiarPasswordWindow(CambiarPasswordViewModel vm, bool obligatorio)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        if (!obligatorio)
            TxtAviso.Text = "Elegí tu nueva contraseña.";
    }

    private async void AlGuardar(object sender, RoutedEventArgs e)
    {
        if (await _vm.CambiarAsync(TxtActual.Password, TxtNueva.Password, TxtConfirmacion.Password))
        {
            MessageBox.Show("La contraseña se cambió correctamente.", "Cambiar contraseña",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
    }
}
