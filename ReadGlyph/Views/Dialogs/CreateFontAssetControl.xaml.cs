using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReadGlyph.Views.Dialogs;

public partial class CreateFontAssetControl : UserControl
{
    /// <summary>ASCII 可打印字符（95个）</summary>
    public const string AsciiPrintable =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public CreateFontAssetControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BtnGenerate.Click += (_, _) =>
            {
                if (SelectedSourceId == null)
                {
                    AlertDialog.Show("提示", "请选择字体源");
                    CmbFontSource.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(OutputName))
                {
                    AlertDialog.Show("提示", "请输入输出文件名");
                    TxtOutputName.Focus();
                    return;
                }
                Confirmed?.Invoke();
            };
            // 勾选状态变化时更新提示
            ChkIncludeAscii.Checked += (_, _) => UpdateHint();
            ChkIncludeAscii.Unchecked += (_, _) => UpdateHint();
        };
    }

    /// <summary>用户点击「生成字体」时触发</summary>
    public event Action? Confirmed;

    /// <summary>在对话框中快速导入字体时触发，参数为源文件路径</summary>
    public event Action<string>? QuickImportFont;

    /// <summary>选中的字体源 ID（需外部设置 ComboBox ItemsSource）</summary>
    public string? SelectedSourceId => CmbFontSource.SelectedValue as string;

    /// <summary>字号</summary>
    public int GlyphFontSize => int.TryParse(TxtSize.Text, out var v) ? v : 16;

    /// <summary>位深</summary>
    public int Bpp => CmbBpp.SelectedIndex switch { 0 => 1, 1 => 2, 2 => 4, 3 => 8, _ => 4 };

    /// <summary>输出 .c 文件名</summary>
    public string OutputName => TxtOutputName.Text.Trim();

    /// <summary>输出子目录（相对路径）</summary>
    public string OutputDir => TxtOutputDir.Text.Trim();

    /// <summary>导出格式</summary>
    public string ExportFormat => "LVGL9";

    /// <summary>最终字符集：ASCII（若勾选）+ 自定义字符</summary>
    public string Glyphs =>
        (ChkIncludeAscii.IsChecked == true ? AsciiPrintable : "")
        + TxtCustomGlyphs.Text;

    private void UpdateHint()
    {
        var count = Glyphs.Length;
        // 可以在状态显示，这里用 ToolTip 简单处理
    }

    private void BtnPickFont_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择字体文件",
            Filter = "字体文件|*.ttf;*.otf;*.ttc|所有文件|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            QuickImportFont?.Invoke(dlg.FileName);
        }
    }
}
