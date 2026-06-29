using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReadGlyph.Views.Dialogs;

public partial class ImportFontSourceControl : UserControl
{
    public ImportFontSourceControl()
    {
        InitializeComponent();
        Loaded += (_, _) => BtnImport.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                AlertDialog.Show("提示", "请选择字体文件");
                return;
            }
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                AlertDialog.Show("提示", "请输入字体显示名称");
                TxtDisplayName.Focus();
                return;
            }
            Confirmed?.Invoke();
        };
    }

    /// <summary>用户点击「导入」时触发</summary>
    public event Action? Confirmed;

    /// <summary>字体显示名称</summary>
    public string DisplayName => TxtDisplayName.Text.Trim();

    /// <summary>字体源文件路径</summary>
    public string FilePath => TxtFontFile.Text.Trim();

    private void BtnPickFontFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择字体文件",
            Filter = "字体文件|*.ttf;*.otf;*.ttc|TrueType (*.ttf)|*.ttf|OpenType (*.otf)|*.otf|TrueType Collection (*.ttc)|*.ttc|所有文件|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtFontFile.Text = dlg.FileName;
        }
    }
}
