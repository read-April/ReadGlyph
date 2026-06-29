using System;
using System.Windows;
using System.Windows.Controls;

namespace ReadGlyph.Views.Components
{
    public partial class TopBar : UserControl
    {
        private bool _isDark = false;

        public TopBar()
        {
            InitializeComponent();
        }

        /// <summary>用户点击「关于」按钮时触发</summary>
        public event Action? AboutClicked;

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutClicked?.Invoke();
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDark = !_isDark;

            // 移除旧主题字典，插入新主题（DynamicResource 自动刷新全局控件）
            var merged = Application.Current.Resources.MergedDictionaries;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString();
                if (src != null && (src.Contains("Dark.xaml") || src.Contains("Light.xaml")))
                {
                    merged.RemoveAt(i);
                    break;
                }
            }
            merged.Insert(0, new ResourceDictionary
            {
                Source = new Uri(_isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml",
                                 UriKind.Relative)
            });

            // 图标切换
            IconSun.Visibility = _isDark ? Visibility.Visible : Visibility.Collapsed;
            IconMoon.Visibility = _isDark ? Visibility.Collapsed : Visibility.Visible;
            IconBorder.Visibility = _isDark ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
