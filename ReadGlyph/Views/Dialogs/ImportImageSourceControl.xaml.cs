using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReadGlyph.Views.Dialogs;

public partial class ImportImageSourceControl : UserControl
{
    public ImportImageSourceControl()
    {
        InitializeComponent();
        Loaded += (_, _) => BtnImport.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                AlertDialog.Show("提示", "请选择图片文件");
                return;
            }
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                AlertDialog.Show("提示", "请输入图片显示名称");
                TxtDisplayName.Focus();
                return;
            }
            Confirmed?.Invoke();
        };
    }

    /// <summary>用户点击「导入」时触发</summary>
    public event Action? Confirmed;

    /// <summary>图片显示名称</summary>
    public string DisplayName => TxtDisplayName.Text.Trim();

    /// <summary>图片源文件路径</summary>
    public string FilePath => TxtImageFile.Text.Trim();

    private void BtnPickImageFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择图片文件",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|所有文件|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtImageFile.Text = dlg.FileName;
        }
    }
}
