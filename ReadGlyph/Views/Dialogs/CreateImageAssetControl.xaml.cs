using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReadGlyph.Views.Dialogs;

public partial class CreateImageAssetControl : UserControl
{
    public CreateImageAssetControl()
    {
        InitializeComponent();

        // 抖动开关联动（必须在 InitializeComponent 之后挂，否则 TxtDitherState 未就绪）
        TglDither.Checked += TglDither_Changed;
        TglDither.Unchecked += TglDither_Changed;

        Loaded += (_, _) => BtnGenerate.Click += (_, _) =>
        {
            if (SelectedSourceId == null)
            {
                AlertDialog.Show("提示", "请选择图片源");
                CmbImageSource.Focus();
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
    }

    /// <summary>用户点击「生成图片」时触发</summary>
    public event Action? Confirmed;

    /// <summary>在对话框中快速导入图片时触发，参数为源文件路径</summary>
    public event Action<string>? QuickImportImage;

    /// <summary>选中的图片源 ID（需外部设置 ComboBox ItemsSource）</summary>
    public string? SelectedSourceId => CmbImageSource.SelectedValue as string;

    /// <summary>是否启用抖动处理</summary>
    public bool DitherEnabled => TglDither.IsChecked == true;

    /// <summary>颜色格式</summary>
    public string Format => CmbFormat.SelectedIndex switch
    {
        0 => "RGB565",
        1 => "RGB888",
        2 => "ARGB8888",
        3 => "I1",
        _ => "RGB888"
    };

    /// <summary>输出 .c 文件名</summary>
    public string OutputName => TxtOutputName.Text.Trim();

    /// <summary>输出子目录（相对路径）</summary>
    public string OutputDir => TxtOutputDir.Text.Trim();

    /// <summary>导出格式</summary>
    public string ExportFormat => "LVGL9";

    private void BtnPickImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择图片文件",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            QuickImportImage?.Invoke(dlg.FileName);
        }
    }

    private void TglDither_Changed(object sender, RoutedEventArgs e)
    {
        TxtDitherState.Text = TglDither.IsChecked == true ? "开启" : "关闭";
    }
}
