using System.Windows;
using EstacionERP.Desktop.ViewModels;

namespace EstacionERP.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
