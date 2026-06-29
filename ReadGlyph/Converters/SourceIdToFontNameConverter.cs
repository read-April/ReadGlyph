using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using ReadGlyph.ViewModels;

namespace ReadGlyph.Converters;

/// <summary>
/// 将 FontAsset.SourceId 解析为字体源的 DisplayName（预览用）
/// </summary>
public class SourceIdToFontNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string sourceId) return null;

        var vm = (Application.Current.MainWindow?.DataContext as MainViewModel);
        return vm?.FontSources.FirstOrDefault(s => s.Id == sourceId)?.DisplayName;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
