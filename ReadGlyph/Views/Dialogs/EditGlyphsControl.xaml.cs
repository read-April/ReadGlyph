namespace ReadGlyph.Views.Dialogs;

/// <summary>
/// 编辑字形字符集对话框 — ASCII 开关 + 自定义额外字符，修改后自动触发重新生成
/// </summary>
public partial class EditGlyphsControl : System.Windows.Controls.UserControl
{
    /// <summary>ASCII 可打印字符（95个）</summary>
    public const string AsciiPrintable =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public EditGlyphsControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BtnConfirm.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(Glyphs))
                {
                    AlertDialog.Show("提示", "请至少勾选 ASCII 字符集或输入自定义字符");
                    return;
                }
                Confirmed?.Invoke();
            };
        };
    }

    /// <summary>用户点击「确定」时触发</summary>
    public event Action? Confirmed;

    /// <summary>
    /// 完整字符集：ASCII（若勾选）+ 自定义额外字符
    /// 设置时：若值以 ASCII 可打印字符开头，自动勾选并拆分；否则仅填充自定义区
    /// </summary>
    public string Glyphs
    {
        get => (ChkIncludeAscii.IsChecked == true ? AsciiPrintable : "")
               + TxtGlyphs.Text;
        set
        {
            if (!string.IsNullOrEmpty(value) && value.StartsWith(AsciiPrintable))
            {
                ChkIncludeAscii.IsChecked = true;
                TxtGlyphs.Text = value[AsciiPrintable.Length..];
            }
            else
            {
                ChkIncludeAscii.IsChecked = false;
                TxtGlyphs.Text = value ?? "";
            }
        }
    }
}
