using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReadGlyph.Converters;

/// <summary>
/// 当 int 值为 0 时返回 Visible，否则返回 Collapsed（用于空状态占位）
/// </summary>
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count == 0)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
