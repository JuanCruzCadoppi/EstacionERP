using System.Windows;
using System.Windows.Controls;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class InicioView : UserControl
{
    public InicioView() => InitializeComponent();

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        if (DataContext is InicioViewModel vm)
            await vm.CargarAsync();
    }
}
