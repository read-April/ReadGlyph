using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ReadGlyph.ViewModels;

namespace ReadGlyph.Converters;

/// <summary>
/// 将 ImageAsset.SourceId 解析为实际图片的 BitmapImage 缩略图
/// </summary>
public class SourceIdToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string sourceId) return null;

        var vm = (Application.Current.MainWindow?.DataContext as MainViewModel);
        var source = vm?.ImageSources.FirstOrDefault(s => s.Id == sourceId);
        if (source == null) return null;

        var fullPath = Path.Combine(App.DataDir, source.FilePath);
        if (!File.Exists(fullPath)) return null;

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(fullPath);
        bmp.DecodePixelWidth = 40;
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
