using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ReadGlyph.Views.Dialogs;

public partial class AboutDialog : UserControl
{
    public AboutDialog()
    {
        InitializeComponent();

        // 从程序集读取版本号
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            TxtVersion.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    /// <summary>用户点击 X 关闭按钮时触发</summary>
    public event Action? Closed;

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Closed?.Invoke();
    }

    private void EmailLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mailto:yu_april@qq.com",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void RepoLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            var url = (sender as System.Windows.Controls.TextBlock)?.Text ?? "";
            if (!url.StartsWith("http")) url = "https://" + url;
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
