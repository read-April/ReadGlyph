using System.Text;

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
            BtnDedupe.Click += (_, _) =>
            {
                var text = TxtGlyphs.Text;
                if (string.IsNullOrEmpty(text)) return;

                // 去重：跳过换行（仅作排版分隔），其余字符保留首次出现顺序
                var sb = new StringBuilder(text.Length);
                var seen = new HashSet<char>();
                var removed = 0;
                foreach (var ch in text)
                {
                    if (ch is '\r' or '\n') continue;
                    if (seen.Add(ch)) sb.Append(ch);
                    else removed++;
                }

                var result = sb.ToString();
                if (result == text)
                {
                    // 无重复字符
                    AlertDialog.Show("提示", "未发现重复字符，无需去重");
                    return;
                }

                // 确认前不修改文本框，点「确定」才应用去重结果
                var message = removed > 0
                    ? $"检测到 {removed} 个重复字符，是否去重？"
                    : "检测到输入中的换行符，是否清理？";
                if (ConfirmDialog.Show("提示", message))
                {
                    // 应用去重到文本框，并通知调用方保存字符集（不触发重新生成）
                    TxtGlyphs.Text = result;
                    DedupeApplied?.Invoke();
                }
            };

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

    /// <summary>去重结果已应用到文本框时触发（调用方应保存字符集但不重新生成）</summary>
    public event Action? DedupeApplied;

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
