using System.Globalization;
using System.Windows.Data;

namespace EstacionERP.Desktop.Comun;

/// <summary>Invierte un bool (true → false).</summary>
public class NegarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value;
}
