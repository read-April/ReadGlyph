using System.Windows;
using System.Windows.Controls;

namespace ReadGlyph.Helpers
{
    /// <summary>
    /// 为 WPF TextBox 提供 UWP 风格的 PlaceholderText 附加属性。
    /// 需配合修改后的 TextBox ControlTemplate 使用（模板中需包含 PART_Watermark 命名部件）。
    /// </summary>
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits));

        public static string GetPlaceholderText(DependencyObject obj)
            => (string)obj.GetValue(PlaceholderTextProperty);

        public static void SetPlaceholderText(DependencyObject obj, string value)
            => obj.SetValue(PlaceholderTextProperty, value);
    }
}
