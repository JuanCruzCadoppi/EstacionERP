using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class UsuariosView : UserControl
{
    public UsuariosView() => InitializeComponent();

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel vm)
            await vm.CargarAsync();
    }

    private void AlHacerDobleClic(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UsuariosViewModel vm && vm.EditarCommand.CanExecute(null))
            vm.EditarCommand.Execute(null);
    }
}
