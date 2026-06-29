using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReadGlyph.Views.Dialogs;

public partial class AlertDialog : UserControl
{
    public AlertDialog(string title, string message)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtMessage.Text = message;
        Loaded += (_, _) => BtnOk.Click += (_, _) => CloseWindow();
    }

    private void CloseWindow()
    {
        Window.GetWindow(this)?.Close();
    }

    /// <summary>以模态窗口形式显示提示</summary>
    public static void Show(string title, string message)
    {
        var dialog = new AlertDialog(title, message);
        var window = new Window
        {
            Content = dialog,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
