using System.Windows;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    private async void AlIngresar(object sender, RoutedEventArgs e)
    {
        if (await _vm.IngresarAsync(TxtPassword.Password))
        {
            DialogResult = true;
        }
        else
        {
            TxtPassword.Clear();
            TxtPassword.Focus();
        }
    }
}
