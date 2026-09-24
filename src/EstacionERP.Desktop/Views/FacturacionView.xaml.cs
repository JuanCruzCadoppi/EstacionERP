using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop.Views;

public partial class FacturacionView : UserControl
{
    public FacturacionView() => InitializeComponent();

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        if (DataContext is FacturacionViewModel vm)
            await vm.CargarAsync();
    }

    private void AlElegirProducto(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FacturacionViewModel vm && vm.AgregarProductoCommand.CanExecute(null))
            vm.AgregarProductoCommand.Execute(null);
    }

    private void AlHacerDobleClicComprobante(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FacturacionViewModel vm && vm.VerPdfCommand.CanExecute(null))
            vm.VerPdfCommand.Execute(null);
    }
}
