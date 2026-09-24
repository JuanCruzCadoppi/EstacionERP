using System.Windows;
using System.Windows.Controls;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class ConfiguracionFiscalView : UserControl
{
    public ConfiguracionFiscalView() => InitializeComponent();

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionFiscalViewModel vm)
            await vm.CargarAsync();
    }
}
