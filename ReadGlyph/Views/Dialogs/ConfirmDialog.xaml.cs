using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReadGlyph.Views.Dialogs;

/// <summary>
/// 通用确认对话框，匹配应用主题风格
/// </summary>
public partial class ConfirmDialog : System.Windows.Controls.UserControl
{
    public ConfirmDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BtnConfirm.Click += (_, _) => Confirmed?.Invoke();
        };
    }

    /// <summary>用户点击「确定」时触发</summary>
    public event Action? Confirmed;

    /// <summary>对话框标题</summary>
    public string Title
    {
        get => TxtTitle.Text;
        set => TxtTitle.Text = value;
    }

    /// <summary>提示消息</summary>
    public string Message
    {
        get => TxtMessage.Text;
        set => TxtMessage.Text = value;
    }

    /// <summary>确认按钮文字（默认"确定"）</summary>
    public string ConfirmText
    {
        get => BtnConfirm.Content as string ?? "确定";
        set => BtnConfirm.Content = value;
    }

    /// <summary>以模态窗口形式显示确认对话框，返回是否点击「确定」</summary>
    public static bool Show(string title, string message, string confirmText = "确定")
    {
        var dialog = new ConfirmDialog
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText
        };
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
        var confirmed = false;
        dialog.Loaded += (_, _) =>
        {
            dialog.Confirmed += () => { confirmed = true; window.Close(); };
            dialog.BtnCancel.Click += (_, _) => window.Close();
        };
        window.ShowDialog();
        return confirmed;
    }
}
